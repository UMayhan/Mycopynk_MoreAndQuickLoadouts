### [1.1.2] - 6th August 2025
#### Changed
- Refactored Highlighter field accesses to use direct property access instead of reflection, improving performance and code maintainability

---

### [1.1.1] - 5th August 2025
#### Fixed
- Fixed loadout changes occurring when chat is open and loadout wheel keys are pressed. The loadout wheel now properly ignores input when the text chat is active, preventing unintended loadout switches during typing.