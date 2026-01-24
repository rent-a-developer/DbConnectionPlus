#!/bin/bash
# This script deletes all bin and obj folders

ParentDir="$(dirname "$PWD")"

for folder in bin obj; do
    # Find all directories named $folder under $ParentDir
    find "$ParentDir" -type d -name "$folder" 2>/dev/null | while read -r dir; do
        echo "Deleting folder: $dir"
        rm -rf "$dir"
    done
done