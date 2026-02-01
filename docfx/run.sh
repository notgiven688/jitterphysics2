#!/bin/bash
docfx metadata
docfx build
docfx serve _site
