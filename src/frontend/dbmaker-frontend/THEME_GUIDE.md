# DbMaker Frontend Theme Guide

Design tokens (CSS variables) are defined in `src/styles.scss` and applied by `ThemeService`.

- Core variables: --primary-color, --accent-color, --bg-primary, --bg-secondary, --bg-tertiary, --text-primary, --text-secondary, --border-color, --card-bg, --shadow-color
- Light/Dark: Body has `light-theme` or `dark-theme` class; ThemeService updates variables and stores preference in localStorage
- Components: Prefer CSS variables in component SCSS (colors, backgrounds, borders) for consistent theming

## Usage tips

- Use `.modern-card`, `.theme-form-field`, `.theme-button`, `.theme-table` utilities for consistent UI
- Toggle theme via the user menu slide toggle (wired to ThemeService)
- Avoid importing prebuilt Angular Material themes; rely on variables for appearance
