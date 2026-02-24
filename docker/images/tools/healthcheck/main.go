// healthcheck performs an HTTP GET against a URL and exits 0 on 2xx, 1 otherwise.
// Designed to be compiled as a static binary (CGO_ENABLED=0) for scratch containers.
//
// Usage: healthcheck <url>
package main

import (
	"fmt"
	"net/http"
	"os"
	"time"
)

func main() {
	if len(os.Args) < 2 {
		fmt.Fprintln(os.Stderr, "usage: healthcheck <url>")
		os.Exit(1)
	}

	client := &http.Client{Timeout: 5 * time.Second}
	resp, err := client.Get(os.Args[1])
	if err != nil {
		os.Exit(1)
	}
	resp.Body.Close()

	if resp.StatusCode < http.StatusOK || resp.StatusCode >= http.StatusMultipleChoices {
		os.Exit(1)
	}
}
