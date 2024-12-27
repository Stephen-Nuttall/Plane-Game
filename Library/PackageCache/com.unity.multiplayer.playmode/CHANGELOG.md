# Changelog

## [1.3.3] - 2024-12-17

### Added
- Fixed an ArgumentNullException issue that occurs across an MPPM upgrade with an actively set playmode Scenario configuration.

### Fixed 
- Fixed an issue where project settings were not getting propagated to Virtual Players when entering Playmode via the play button

## [1.4.0-pre.1] - 2024-12-05

### Fixed
- Fixed an issue where clone editor VP Windows closes unexpectedly when entering a Playmode Scenario with Multiplayer Windows opened.

## [1.3.2] - 2024-11-14

### Fixed
- Fixed clones remaining active when a play mode scenario is stopped while the clone is activating.
- Fixed an issue where enabling Playmode Tools in a clone editor's layout configuration may display incorrect layouts.
- Fixed compilation error "'UnityPlayer' does not contain a definition for 'Role'" when Dedicated Server package version doesn't match.

### Added
- Avoid reconstructing virtual player windows through Playmodes if their layouts in PlayMode vs EditMode are the same. This helps retain anchoring in Windows.

## [1.3.1] - 2024-10-21

### Fixed

- Fixed an issue where the "Player X failed to sync" window would pop up even though no synchronization issue could be observed.
- Virtual Player folder is now properly refreshed when the package, engine, or engine changeset version is updated.

### Added

- Added a helpbox to the Multiplayer window to prevent package imports while the virtual player(s) are activating. The helpbox will remain visible until all virtual player(s) are ready.
- Added Multiplayer Menu links to the scenario configuration and the Scenarios status under Windows > Multiplayer

## [1.3.0] - 2024-09-26

### Changed

- Updated `com.unity.services.multiplayer` to 1.0.0 release version.
- Bump of the minimum Engine version to 6000.0.22f1 to address a crash happening when using virtual players with a build profile.

## [1.3.0-pre.3] - 2024-09-20

### Changed

- Replaced the **Original Name** property in the **Remote Instances** > **Advance Configuration** section with the **Identifier** property. The **Identifier** string field produces a unique name for the Multiplay Build, Build Configuration, and Fleet. This name is in the form `CreatedFromTheUnityEditor-[identifier]-[username]`.

### Added

- Added stream logs to main editor option for additional editor instances
- Fixed streaming logs from local instances stopping when domain reloads are enabled
- Added percentage progress to the Playmode Status Window for the Preparing, Deploying, and Launching stages

### Fixed

- Removed compilation warnings "This async method lacks 'await' operators and will run synchronously"
- Fixed streaming logs from remote instances not working.
- Fixed code changes not consistently syncing properly between the main editor and the clone when using Rider
- Fixed the asset out of sync error caused by the clone not starting in the correct build target

## [1.3.0-pre.2] - 2024-08-14

### Fixed
- Fixed MPPM virtual player exiting play mode by calling OpenScene() and CloseScene() during play mode errors when multiple scenes are present/switching in the project.
- Fixed "type is not a supported string value" error when adding a tag to the main editor instance
- Fixed tags not persisting in the tag dropdown after navigating away from the scenario config window
- Fixed  Mppm can't spawn virtual player in playmode

### Changed
- MPPM Window is now disabled when there is a scenario actively selected in the dropdown

## [1.3.0-pre.1] - 2024-07-26

### Changed
- Improved time it takes to enter play mode with remote instances on consecutive runs when they produce equivalent build files.
- Main editor instance is now optional.
- Updated the remote deployment dependency to `com.unity.services.multiplayer@1.0.0-pre.1`
- Removed UPM from the clones to improve performance and so clones rely on library redirect as originally intended

### Fixed
- Added a modal offering “Save,” “Don’t Save,” and “Cancel” options when initiating a virtual player with an unsaved scene
- Fixed Asset database out of sync error when launching a clone

## [1.3.0-exp.4] - 2024-07-16

### Changed
- Updated the remote deployment dependency to `com.unity.services.multiplayer@0.6.0`

## [1.3.0-exp.3] - 2024-07-11

### Changed
- Moved the Multiplayer Window menu item from "Window/Multiplayer Playmode" to "Window/Multiplayer/Multiplayer Playmode" to ensure consistency with the other Multiplayer packages.
- Scenario status popup title changed from "Connection Status" to "Instances Status".

### Added
- Added a new Playmode Status Window that gives more information about each instance in the scenario

### Fixed
- Removed warning log when the multiplay package is not installed. Information message now appears only in the UI in the "configure play mode scenarios" window.
- Fixed remote instances default arguments from `-log` to `-logFile`.

## [1.3.0-exp.2] - 2024-07-03

### Fixed
- Fixed "Stream logs to main editor" option not working in local instances.

## [1.3.0-exp.1] - 2024-06-24

### Added
- Extending Playmode with scenarios configuration capabilities. Allowing orchestration of in editor, local and remote instances.

### Fixed
- No longer hit an exception when launching the standalone profiler

## [1.2.0] - 2024-06-04

### Added
- Added tool tips to the preferences window
- Flatten the MPPM preferences window, also changed first checkbox wording

### Fixed
- Fixed Multiplayer roles not displaying after reinstalling Dedicated Server Package
- Fixed main view window, so it may not be shrunk destructively
- Fixed the tooltip auto-close issue, so it now persists long enough to be useful.


## [1.1.0] - 2024-04-24

### Added
- Added Netcode For Entities layout option to each mppm clone if package is installed.
- Added the ability to mute players to the settings
- Added the ability to change the asset database refresh timeout to the settings
- Can now properly focus on other individual player windows from inside an individual player (by using the keyboard shortcuts)
- Added clearer message to certain types of Symlink failures (on FAT32) which are not supported

### Fixed
- Upgrades of the MPPM package now clear out the local clone cache to ensure stable updates
- Fixed a crash with layout files when computers were set to certain regions
- Changed default multiplayer role of player clones to Client and Server
- Added a minimum width for the main view of the MPPM window
- Escape key no longer closes virtual player windows on Windows
- Fixed issues with heartbeat timeout
- No longer allow '/' as part of tags as these types of slashes are intended for drop down behavior

## [1.0.0] - 2024-03-12

### Added

- Multiplayer development workflow aiming to offer a more efficient and quick development cycle of multiplayer games. The tool enables opening multiple Unity Editor instances simultaneously on the same development device, using the same source assets on disk.
