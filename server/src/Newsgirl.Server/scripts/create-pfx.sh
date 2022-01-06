#!/usr/bin/env bash

OUT_PATH=$1

if [[ -z "$OUT_PATH" ]]; then
  echo "Please provide an output path as first parameter.";
  exit 1;
fi

set -exu

# Generate a private key.
openssl genpkey -algorithm RSA -out /tmp/private_key.key -pkeyopt rsa_keygen_bits:4096

# Generate a public key.
openssl rsa -in /tmp/private_key.key -pubout > /tmp/public_key.pub

# Generate a certificate.
openssl req -key /tmp/private_key.key -new -nodes -x509 -out /tmp/certificate.crt -subj "/C=EU/ST=xdxd/L=xdxd/O=xdxd/CN=xdxd.eu"
openssl pkcs12 -export -out /tmp/certificate.pfx -inkey /tmp/private_key.key -in /tmp/certificate.crt -passout pass:

# Remove everything but the .pfx file.
rm /tmp/private_key.key /tmp/public_key.pub /tmp/certificate.crt

# Move the .pfx file where we need it.
mv /tmp/certificate.pfx "$OUT_PATH"
