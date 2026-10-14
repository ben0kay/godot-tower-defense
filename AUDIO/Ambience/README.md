# Ambience

Global/non-positional space atmosphere belongs here (for example a quiet background bed). Use `Category = Ambience` and a Loop definition. Localized portal, Citadel, enemy or tower ambience lives under the **owning feature's** `Sounds/` folder and can use a positional Loop or Intermittent definition.

The central audio manager handles distance, offscreen rules, loop budgets, fades and stale owner cleanup. No ambient clips or gameplay ambience transitions have been added yet.
