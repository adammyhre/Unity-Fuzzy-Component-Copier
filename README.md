# Unity Fuzzy Component Copier

An intelligent component value copying tool for Unity that uses fuzzy matching to copy values between components with different field names.

## Features

- **Intelligent Matching**: Automatically matches fields between different component types using multiple scoring strategies
- **Flexible Algorithms**: Choose between Greedy (fast) or Hungarian (optimal) matching algorithms
- **Default Mappings**: Pre-configured mappings for common field name variations (Speed ↔ Velocity, Health ↔ HitPoints, etc.)
- **User-Defined Aliases**: Create custom field mappings that persist across sessions
- **Polymorphic Support**: Handles managed references with runtime type information
- **Preview Window**: See all matches with confidence scores before pasting
- **Undo Support**: All operations are fully undoable
- **Type Safety**: Validates type compatibility and provides smart type coercion

## Example Usage

1. **Copy values from one component:**
   - Right-click on a component in the Inspector
   - Select "Fuzzy Copy Values"

2. **Paste values to another component:**
   - Right-click on a different component (can be different type)
   - Select "Fuzzy Paste Values"
   - Review the matches in the preview window
   - Click "Paste" on individual matches or "APPLY ALL MATCHES" to paste everything

3. **Create custom aliases:**
   - In the paste preview window, click "Teach Permanent Alias"
   - Select a match to create a permanent mapping
   - Future pastes will recognize this mapping automatically

## Matching Strategies

The tool uses multiple scoring strategies in priority order:

1. **Exact Path Match** (100%): Perfect match when property paths are identical
2. **Exact Name Match** (98%/90%): Field names match exactly
3. **User Aliases** (95%/75%): Custom mappings you've created
4. **Default Mappings** (92%/70%): Pre-configured common mappings
5. **FormerlySerializedAs** (90%/65%): Handles renamed fields
6. **Levenshtein Similarity** (Variable): Fuzzy name matching with type compatibility checks

## Matching Algorithms

The tool supports two algorithms for finding optimal field assignments:

- **Greedy Algorithm** (Default): Fast algorithm that selects the best matches in order. Works well for most cases and is recommended for components with many fields.
- **Hungarian Algorithm**: Optimal algorithm that guarantees the best overall matching. Slower for large numbers of fields but ensures maximum total match score.

### Switching Algorithms

To change the algorithm preference:

1. Open **Edit → Preferences** (Windows/Linux) or **Unity → Preferences** (Mac)
2. Navigate to **Tools → Fuzzy Component Copier**
3. Toggle **"Use Hungarian Algorithm"** on or off
4. Your preference is saved and will be used for all future paste operations

**Note**: The Greedy algorithm is the default for faster performance. Use Hungarian when you need optimal matching and don't mind waiting a few seconds for complex components.

## Default Mappings

The tool includes default mappings for common field name variations:

- `Speed` ↔ `Velocity`, `MoveSpeed`, `MaxSpeed`
- `Health` ↔ `HitPoints`, `HP`, `Life`
- `Position` ↔ `Location`, `Pos`
- `Damage` ↔ `AttackPower`, `Attack`, `Power`
- `CharacterName` ↔ `Title`, `Name`
- `IsMoving` ↔ `CanMove`, `Moving`
- `IsActive` ↔ `Enabled`, `Active`
- `TeamColor` ↔ `FactionColor`, `Color`

You can edit these mappings in the `FuzzyCopierAliases` ScriptableObject asset.

## How to Install

### Option 1: Unity Package Manager (Recommended)

1. Open Unity Package Manager (Window → Package Manager)
2. Click the "+" button → "Add package from git URL"
3. Enter: `https://github.com/adammyhre/Unity-Fuzzy-Component-Copier.git`

### Option 2: Add to Manifest

Add the following line to your project's `Packages/manifest.json` file:

```json
"com.gitamend.fuzzycomponentcopier": "https://github.com/adammyhre/Unity-Fuzzy-Component-Copier.git"
```

### Option 3: Manual Installation

1. Download or clone this repository
2. Copy the `Unity-Fuzzy-Component-Copier` folder into your `Assets` folder
3. The tool will be available immediately

## Requirements

- Unity 2020.3 or later
- No additional dependencies required

## Notes

- All operations are undoable (Ctrl+Z / Cmd+Z)
- The matching algorithm ensures one-to-one field matching (no duplicates)
- Scores are capped at 100% - only exact path matches can achieve 100%
- Default mappings are stored in a ScriptableObject and can be customized
- User aliases persist across Unity sessions

## YouTube

Check out the git-amend [YouTube channel](https://www.youtube.com/@git-amend?sub_confirmation=1) for more Unity content.
