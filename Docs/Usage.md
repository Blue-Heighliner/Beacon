# Usage

How to run and use the app in everyday situations.

## First run

The app creates its database and a built-in administrator account the first time it starts:

| Username | Password |
| --- | --- |
| `admin` | `TestAdmin123` |

Change this password immediately under My Account, Change Password. The default is hardcoded for initial setup and is not a secret.

## Adding items

Barcode scanners type the code and press Enter, so the entry fields are built around Enter:

1. Scan the serial number. Enter moves to the item name, selecting any text already there.
2. Type or keep the item name, then press Enter to move to the category.
3. Type or keep the category, then press Enter to save. Focus returns to the serial field.

Name and category persist between scans so a shelf of identical gear can be batch scanned with a bare Enter on each field. A duplicate serial is rejected with a status message.

## Filtering and exporting

Filter the grid by category, by the user who added the item, and by a local date range. Export either the filtered rows or everything to an `.xlsx` workbook from the export buttons.

## Accounts

Anyone can register an account with a password, a badge scan, or both, plus a 10-digit DOD ID. Users can change their own password and badge. Administrators can edit usernames and DOD IDs, reset passwords and badges, and delete non-admin accounts. Non-admins can delete only items they added.

## Where data lives

```
%LOCALAPPDATA%\Beacon\inventory.db        (Windows)
~/.local/share/Beacon/inventory.db        (Linux)
```

The location is per OS user and per machine, so each machine holds its own separate inventory. To back up or transfer data, copy `inventory.db` (and `dodid.key` on Linux). To reset an installation completely, delete the folder; the app rebuilds it, and the default admin, on next launch.
