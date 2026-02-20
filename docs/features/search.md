# Search

Quickly find any resource using the integrated Visual Studio search box in the tool window. Just start typing to search across all your signed-in accounts and subscriptions simultaneously.

## Features

- **Instant Results** — Cached resources appear immediately as you type
- **Background Search** — Azure API search runs in parallel for comprehensive results
- **Grouped by Account** — Results are organized by Account → Subscription for easy navigation
- **All Resource Types** — Searches App Services, Function Apps, VMs, Storage, Key Vaults, SQL, and more
- **Tag Search** — Use `tag:Key=Value` syntax to find resources by tag (case-insensitive)

## Search Syntax

| Query                        | Description                                       |
| ---------------------------- | ------------------------------------------------- |
| `myapp`                      | Find resources with "myapp" in the name           |
| `tag:Environment=Production` | Find resources tagged with Environment=Production |
| `tag:environment=production` | Same as above (case-insensitive)                  |
| `tag:Team`                   | Find resources with any value for the Team tag    |
| `api tag:Environment=Dev`    | Combine name and tag filters                      |

## Quick Tag Filter

You can also right-click on any tag in the tree and select **"Filter by This Tag"** to quickly search for all resources with that tag.

## See Also

- [Resource Tags](tags.md)
