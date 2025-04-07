#!/bin/bash

# Input to be sent to the binary (change this to suit your program)
MODE="1"
SITE="1"

# Path to your binary (change this if needed)
BINARY_PATH="./job-crawler"

# Run the binary and feed the input
"$BINARY_PATH" <<EOF
$MODE
$SITE
EOF