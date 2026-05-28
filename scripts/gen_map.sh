#!/bin/bash
# Run this before starting a Claude Code session to give Claude spatial awareness
# Usage: bash scripts/gen_map.sh

OUTPUT="project_map.txt"
echo "=== Project Map — $(date) ===" > $OUTPUT

if [ -d "Assets/Scripts" ]; then
  echo "" >> $OUTPUT
  echo "## Unity Scripts" >> $OUTPUT
  tree Assets/Scripts/ -I "Plugins|Editor|*.meta" --dirsfirst >> $OUTPUT 2>/dev/null || find Assets/Scripts -name "*.cs" | sort >> $OUTPUT
fi

if [ -d "src" ]; then
  echo "" >> $OUTPUT
  echo "## Source" >> $OUTPUT
  tree src/ --dirsfirst -I "node_modules|dist|*.js.map" >> $OUTPUT 2>/dev/null || find src -type f | sort >> $OUTPUT
fi

echo "" >> $OUTPUT
echo "Generated: $OUTPUT"
