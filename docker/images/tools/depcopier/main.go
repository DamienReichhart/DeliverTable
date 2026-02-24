// depcopier discovers shared library dependencies of ELF binaries and assembles
// a minimal rootfs directory suitable for scratch-based Docker images.
//
// It reads DT_NEEDED entries and PT_INTERP from ELF headers (no ldd dependency),
// recursively resolves every transitive shared library, then mirrors them into an
// output directory preserving paths and symlinks.
//
// Additional flags allow copying arbitrary files, creating symlinks, injecting CA
// certificates, and generating minimal /etc/passwd + /etc/group files.
//
// Usage:
//
//	depcopier --out /rootfs \
//	  --scan /staging/app \
//	  --copy /staging/app:/app \
//	  --copy /staging/healthcheck:/healthcheck \
//	  --link /dev/stdout:/var/log/nginx/access.log \
//	  --certs \
//	  --user 1000:1000:appuser \
//	  --mkdir /tmp
package main

import (
	"debug/elf"
	"fmt"
	"io"
	"io/fs"
	"os"
	"path/filepath"
	"strings"
)

var libSearchDirs = []string{
	"/lib",
	"/lib64",
	"/usr/lib",
	"/usr/lib64",
	"/usr/local/lib",
}

// ── Entry Point ──────────────────────────────────────────────

func main() {
	cfg := mustParseArgs(os.Args[1:])

	deps := map[string]bool{}
	for _, root := range cfg.scanPaths {
		for _, path := range discoverELFFiles(root) {
			resolveDeps(path, deps)
		}
	}

	for p := range deps {
		if err := mirrorPath(p, cfg.outDir); err != nil {
			fmt.Fprintf(os.Stderr, "depcopier: warn: mirror %s: %v\n", p, err)
		}
	}

	for _, spec := range cfg.copySpecs {
		src, dst := mustSplitSpec(spec)
		if err := deepCopy(src, filepath.Join(cfg.outDir, dst)); err != nil {
			fatal("copy %s -> %s: %v", src, dst, err)
		}
	}

	for _, spec := range cfg.linkSpecs {
		target, linkPath := mustSplitSpec(spec)
		abs := filepath.Join(cfg.outDir, linkPath)
		must(os.MkdirAll(filepath.Dir(abs), 0o755))
		must(os.Symlink(target, abs))
	}

	if cfg.certs {
		installCACerts(cfg.outDir)
	}

	if cfg.userSpec != "" {
		installUserGroup(cfg.userSpec, cfg.outDir)
	}

	for _, d := range cfg.mkdirs {
		must(os.MkdirAll(filepath.Join(cfg.outDir, d), 0o755))
	}

	fmt.Fprintf(os.Stderr, "depcopier: rootfs ready at %s\n", cfg.outDir)
}

// ── Configuration ────────────────────────────────────────────

type config struct {
	scanPaths []string
	copySpecs []string // "src:dst" pairs
	linkSpecs []string // "target:linkpath" pairs
	mkdirs    []string
	outDir    string
	certs     bool
	userSpec  string // "uid:gid:name"
}

func mustParseArgs(args []string) config {
	var cfg config
	for i := 0; i < len(args); i++ {
		switch args[i] {
		case "--scan":
			i++
			cfg.scanPaths = append(cfg.scanPaths, args[i])
		case "--copy":
			i++
			cfg.copySpecs = append(cfg.copySpecs, args[i])
		case "--link":
			i++
			cfg.linkSpecs = append(cfg.linkSpecs, args[i])
		case "--mkdir":
			i++
			cfg.mkdirs = append(cfg.mkdirs, args[i])
		case "--out":
			i++
			cfg.outDir = args[i]
		case "--certs":
			cfg.certs = true
		case "--user":
			i++
			cfg.userSpec = args[i]
		default:
			fatal("unknown flag: %s", args[i])
		}
	}
	if cfg.outDir == "" {
		fatal("--out is required")
	}
	return cfg
}

// ── ELF Discovery ────────────────────────────────────────────

func discoverELFFiles(root string) []string {
	info, err := os.Stat(root)
	if err != nil {
		fatal("stat %s: %v", root, err)
	}
	if !info.IsDir() {
		if isELF(root) {
			return []string{root}
		}
		return nil
	}

	var result []string
	filepath.WalkDir(root, func(p string, d fs.DirEntry, err error) error {
		if err != nil || d.IsDir() {
			return nil
		}
		if d.Type().IsRegular() && isELF(p) {
			result = append(result, p)
		}
		return nil
	})
	return result
}

func isELF(path string) bool {
	f, err := os.Open(path)
	if err != nil {
		return false
	}
	defer f.Close()
	var magic [4]byte
	if _, err := io.ReadFull(f, magic[:]); err != nil {
		return false
	}
	return magic == [4]byte{0x7f, 'E', 'L', 'F'}
}

// ── Dependency Resolution ────────────────────────────────────

func resolveDeps(path string, found map[string]bool) {
	realPath, err := filepath.EvalSymlinks(path)
	if err != nil {
		realPath = path
	}
	if found[realPath] {
		return
	}

	f, err := elf.Open(realPath)
	if err != nil {
		return
	}
	defer f.Close()

	for _, prog := range f.Progs {
		if prog.Type == elf.PT_INTERP {
			buf := make([]byte, prog.Filesz)
			if _, err := prog.ReadAt(buf, 0); err == nil {
				interp := strings.TrimRight(string(buf), "\x00")
				if interp != "" {
					markWithSymlinks(interp, found)
					resolveDeps(interp, found)
				}
			}
			break
		}
	}

	libs, err := f.ImportedLibraries()
	if err != nil {
		return
	}
	for _, name := range libs {
		libPath := findLibrary(name)
		if libPath == "" {
			fmt.Fprintf(os.Stderr, "depcopier: warn: library %s not found\n", name)
			continue
		}
		markWithSymlinks(libPath, found)
		resolveDeps(libPath, found)
	}
}

// markWithSymlinks adds a path and its resolved real path to the set.
func markWithSymlinks(p string, found map[string]bool) {
	found[p] = true
	real, err := filepath.EvalSymlinks(p)
	if err == nil && real != p {
		found[real] = true
	}
}

func findLibrary(name string) string {
	for _, dir := range libSearchDirs {
		p := filepath.Join(dir, name)
		if _, err := os.Lstat(p); err == nil {
			return p
		}
	}
	return ""
}

// ── File Mirroring ───────────────────────────────────────────

// mirrorPath recreates src inside outDir at the same absolute path,
// faithfully preserving symlinks and following them recursively.
func mirrorPath(src, outDir string) error {
	dst := filepath.Join(outDir, src)
	if _, err := os.Lstat(dst); err == nil {
		return nil
	}
	must(os.MkdirAll(filepath.Dir(dst), 0o755))

	info, err := os.Lstat(src)
	if err != nil {
		return err
	}

	if info.Mode()&os.ModeSymlink != 0 {
		target, err := os.Readlink(src)
		if err != nil {
			return err
		}
		if err := os.Symlink(target, dst); err != nil {
			return err
		}
		var abs string
		if filepath.IsAbs(target) {
			abs = target
		} else {
			abs = filepath.Join(filepath.Dir(src), target)
		}
		return mirrorPath(abs, outDir)
	}

	return copyFile(src, dst)
}

// deepCopy copies a file or directory tree, preserving symlinks.
func deepCopy(src, dst string) error {
	info, err := os.Lstat(src)
	if err != nil {
		return err
	}
	if !info.IsDir() {
		must(os.MkdirAll(filepath.Dir(dst), 0o755))
		return copyFile(src, dst)
	}

	return filepath.WalkDir(src, func(p string, d fs.DirEntry, walkErr error) error {
		if walkErr != nil {
			return walkErr
		}
		rel, _ := filepath.Rel(src, p)
		target := filepath.Join(dst, rel)

		if d.Type()&fs.ModeSymlink != 0 {
			link, err := os.Readlink(p)
			if err != nil {
				return err
			}
			must(os.MkdirAll(filepath.Dir(target), 0o755))
			return os.Symlink(link, target)
		}

		if d.IsDir() {
			return os.MkdirAll(target, 0o755)
		}

		must(os.MkdirAll(filepath.Dir(target), 0o755))
		return copyFile(p, target)
	})
}

func copyFile(src, dst string) error {
	sf, err := os.Open(src)
	if err != nil {
		return err
	}
	defer sf.Close()

	info, err := sf.Stat()
	if err != nil {
		return err
	}

	df, err := os.OpenFile(dst, os.O_CREATE|os.O_WRONLY|os.O_TRUNC, info.Mode().Perm())
	if err != nil {
		return err
	}
	defer df.Close()

	_, err = io.Copy(df, sf)
	return err
}

// ── CA Certificates ──────────────────────────────────────────

func installCACerts(outDir string) {
	const bundle = "/etc/ssl/certs/ca-certificates.crt"
	if _, err := os.Stat(bundle); err != nil {
		fmt.Fprintln(os.Stderr, "depcopier: warn: CA certificate bundle not found")
		return
	}
	dst := filepath.Join(outDir, bundle)
	must(os.MkdirAll(filepath.Dir(dst), 0o755))
	if err := copyFile(bundle, dst); err != nil {
		fatal("certs: %v", err)
	}
}

// ── User / Group ─────────────────────────────────────────────

func installUserGroup(spec, outDir string) {
	parts := strings.SplitN(spec, ":", 3)
	if len(parts) != 3 {
		fatal("--user expects uid:gid:name")
	}
	uid, gid, name := parts[0], parts[1], parts[2]

	etc := filepath.Join(outDir, "etc")
	must(os.MkdirAll(etc, 0o755))

	passwd := fmt.Sprintf(
		"root:x:0:0:root:/root:/sbin/nologin\n%s:x:%s:%s::/home/%s:/sbin/nologin\n",
		name, uid, gid, name,
	)
	group := fmt.Sprintf("root:x:0:\n%s:x:%s:\n", name, gid)

	must(os.WriteFile(filepath.Join(etc, "passwd"), []byte(passwd), 0o644))
	must(os.WriteFile(filepath.Join(etc, "group"), []byte(group), 0o644))
}

// ── Helpers ──────────────────────────────────────────────────

func mustSplitSpec(s string) (string, string) {
	parts := strings.SplitN(s, ":", 2)
	if len(parts) != 2 {
		fatal("invalid spec %q (expected left:right)", s)
	}
	return parts[0], parts[1]
}

func must(err error) {
	if err != nil {
		fatal("%v", err)
	}
}

func fatal(format string, args ...any) {
	fmt.Fprintf(os.Stderr, "depcopier: "+format+"\n", args...)
	os.Exit(1)
}
