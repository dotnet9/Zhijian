# Changelog

## 2026-05-16

- Removed `Avalonia.Themes.Fluent` so the desktop app now runs on AtomUI styling only.
- Replaced remaining native text editors with AtomUI text controls, fixing invisible editor text after removing Fluent.
- Rebuilt the title-bar file menu as a real AtomUI `WindowTitleBar` add-on so it stays out of the work area while remaining clickable.
- Restored the title-bar light/dark theme switch and connected the custom outline and mind-map surfaces to the same theme state.
- Polished the title bar branding, outline/Markdown switch, and mind-map zoom controls, then verified the main workflow with running-app screenshots.
