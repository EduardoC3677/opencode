---
name: get-search-view-results
description: Reconstruct current workspace search results using OpenCode search/read tools when a UI search panel is not directly accessible.
compatibility: opencode
---
# Getting search results in OpenCode

OpenCode does not document a universal tool for scraping an editor Search panel.
When the user asks for "the current search results", reproduce them with the workspace search tools instead.

## Procedure

1. Confirm or infer the search term, glob, and directory scope.
2. Use OpenCode's file listing and content search tools to rerun the search in the workspace.
3. Return the result as:
   - matching files
   - matching lines or counts
   - any filters or assumptions used
4. If the user literally means an editor UI panel and that panel is not accessible from the current environment, say so clearly and provide an equivalent workspace search result instead.
