FROM cloudflare/cloudflared:latest

# Token is read from the TUNNEL_TOKEN environment variable at runtime.
ENTRYPOINT ["cloudflared", "tunnel"]
CMD ["--no-autoupdate", "run"]
