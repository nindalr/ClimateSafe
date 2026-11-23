#!/bin/sh
set -eu

# A CA certificate is public, but write it with restricted permissions and never log its contents.
if [ -n "${SUPABASE_CA_CERT:-}" ]; then
    case "$SUPABASE_CA_CERT" in
        *"-----BEGIN CERTIFICATE-----"*"-----END CERTIFICATE-----"*) ;;
        *) echo "SUPABASE_CA_CERT must contain a PEM certificate." >&2; exit 1 ;;
    esac
    umask 077
    certificate_file=$(mktemp /tmp/climatesafe-ca.XXXXXX)
    printf '%s\n' "$SUPABASE_CA_CERT" > "$certificate_file"
    export PGSSLROOTCERT="$certificate_file"
fi

exec dotnet ClimateSafe.Api.dll
