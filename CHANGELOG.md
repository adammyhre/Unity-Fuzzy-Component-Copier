# Changelog

All notable changes to this project will be documented in this file.

## [1.0.0] - 2025-11-29

### Added
- Initial release of Unity Fuzzy Component Copier
- Fuzzy matching algorithm for copying component values between different component types
- Hungarian algorithm (Munkres) for optimal assignment matching
- Support for default field name mappings (e.g., Speed ↔ Velocity, Health ↔ HitPoints)
- User-defined alias system with ScriptableObject persistence
- Support for polymorphic managed references
- Levenshtein distance algorithm for name similarity matching
- Context menu integration: "Fuzzy Copy Values" and "Fuzzy Paste Values"
- Preview window showing match scores and allowing selective pasting
- Support for all common Unity serialized property types (int, float, bool, string, Vector3, Color, etc.)
- Undo support for all paste operations
- Test components with reset functionality for easy manual testing

