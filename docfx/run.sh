#!/bin/bash
set -e
cd "$(dirname "$0")"
docfx metadata
docfx build
docfx serve _site
