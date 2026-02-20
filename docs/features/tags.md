# Resource Tags

Organize and filter your Azure resources using tags.

## Features

- **View Tags** — Expand any resource to see its tags as key-value pairs
- **Add Tags** — Right-click a resource or the Tags node and select "Add Tag..."
- **Remove Tags** — Right-click any tag and select "Remove Tag" to delete it
- **Search by Tag** — Use `tag:Key=Value` syntax in the search box to find resources
- **Filter by Tag** — Right-click a tag and select "Filter by This Tag" to search
- **Copy Tags** — Copy all tags as JSON or individual tag values
- **Auto-complete** — Tag keys and values auto-populate from previously used tags

## Tag Search Syntax

| Query                        | Description                                       |
| ---------------------------- | ------------------------------------------------- |
| `tag:Environment=Production` | Find resources tagged with Environment=Production |
| `tag:environment=production` | Same as above (case-insensitive)                  |
| `tag:Team`                   | Find resources with any value for the Team tag    |
| `api tag:Environment=Dev`    | Combine name and tag filters                      |

## See Also

- [Search](search.md)
