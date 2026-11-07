# UI Visual Identity — Concept Brainstorm

> **Status: Brainstorming / visual direction only. Not approved for implementation.**
>
> This note captures the desired look and feel for a future, game-wide interface refresh in Godot Tower Defense. It intentionally **does not** lock down a colour palette, theme values, layout measurements, code architecture, implementation phases or release schedule. Future design documents can build on these ideas after visual mockups and in-game review.
>
> The goal is to describe **how the interface should feel** before deciding how to build it.

## Overall vision

Aim for a coherent **dark, futuristic tactical-command interface** across the game: something that feels like sophisticated military or spacecraft software rather than a collection of ordinary rectangular Godot controls.

The interface should feel **engineered, precise, atmospheric, high-tech and responsive**, with a restrained sense of machinery and information coming to life. There can be detail and movement, but it must remain a useful interface in a busy tower-defense battle.

The new look should be recognizable throughout the Boot/Main menus, Campaign, Research, build menu, selected-tower Inspector, HUD, Pause, loading, narration, alerts and Results, while allowing each screen to have a different density and purpose.

## Visual ideas to explore

### 1. Dark layered surfaces

- Primarily dark-themed panels, potentially with layered surfaces that feel like glass, dark metal, or translucent command-console screens.
- A hierarchy of background, panel surface, inset section, active selection and floating overlay; avoid everything becoming one flat rectangle.
- **Subtle blur/frosted-background treatments** behind selected panels where visually useful, without obscuring battlefield readability.
- Fine texture, faint gradients or understated environmental glow are possibilities—not requirements.
- Transparent and opaque panels can coexist: battlefield HUD panels should reveal enough of the game without compromising text contrast.

**Open:** Decide which areas suit true blur, simulated blur, dimming or simple opacity only after inspecting visuals and performance.

### 2. Sharp vector geometry and corners

- Replace some conventional boxes with **angular, chamfered, cut-out or jagged corner profiles**, as though the interface was fabricated from vector shapes.
- Thin traced edges, brackets, clipped corners, offset corner strokes, short segmented frame sections and nested outlines.
- Avoid making every panel a fully enclosed rectangle. Some frames could use partial borders and corner anchors instead.
- Small technical linework can communicate panel divisions, selected states and system hierarchy.
- Keep a consistent visual vocabulary across screens, but vary complexity between large menus and compact tactical controls.

### 3. Glowing edges and energy accents

- Selective **luminous outlines**, edge bloom/halos, fine inner highlights and occasional brighter emphasis on active controls.
- Different strength of glow for idle, hover, focus, selected, warning, disabled and success states.
- A few illuminated vector segments can feel more sophisticated than making whole panels glow.
- Ideally the glow enhances the silhouette and helps the player identify actionable UI; it must not wash out labels or compete with combat effects.
- No final colour assignments yet. A dark base and luminous tactical accents are the direction, not a committed palette.

### 4. Motion inspired by construction telemetry

The existing **circular construction-progress telemetry** is an important visual reference: rotating curved lines, counter-rotation, orbiting dots and a climbing number suggest a larger family of restrained, procedural sci-fi UI motion.

Possible motifs:

- Small counter-rotating arcs or tracking segments near loading, scanning, construction and analysis indicators.
- Tiny particles or bright points moving along a border or following short vector paths.
- Tracing lines that sweep briefly across an opening panel, then settle.
- Edge segments that pulse or travel when a selection changes or an important event occurs.
- Small animated brackets that converge around a selected item or reveal new information.
- Gentle progress ticks, segmented rings and sensor-like sweeps in the right contexts.

These are **ideas**, not a request to animate every panel constantly. Animation should be mostly **event-triggered and purposeful**, with calm, mostly stationary idle states. The interface should remain pleasant and legible even when everything is at rest.

### 5. Typography, information hierarchy and detail

- Consistent text styles for major headings, section headings, labels, numbers, prices, statuses and small telemetry.
- Potentially a technical/sci-fi typographic feel, but never at the expense of legibility.
- Clean alignment and predictable spacing, especially for tower statistics, next-upgrade deltas, health/ammo/energy meters and build costs.
- Optional thin dividers, micro-labels, indices, markers or telemetry captions where they convey a useful distinction.
- Important actions should stand out clearly even with decorative flourishes removed.

**Open:** Choose fonts, sizes, iconography and exact hierarchy after trying visual examples at real game resolutions.

## Shared identity, different panel roles

The overall design language should apply everywhere, **without forcing every screen into the same shape**.

| Interface family | Initial visual direction to explore |
| --- | --- |
| **Large menu surfaces** | Architectural framing, prominent vector corners, deliberate hierarchy and occasional ambient detail |
| **Build menu / tower cards** | Uniform cards, clear thumbnails and prices, strong hover/selected indicators, compact technical accents |
| **Selected-tower Inspector** | Readable dense telemetry, modular stat sections, precise bars and distinct action areas |
| **Top battlefield HUD** | Thin unobtrusive framing, clear Credits/health/waves, minimal ambient motion |
| **Research / Campaign** | More room for futuristic graph/map motifs, selected-path glows and progression accents |
| **Narration / alerts** | Lightweight tactical broadcast style, brief arrivals, high legibility and strong priority cues |
| **Loading / Results** | Space for more cinematic animated vector motifs, while preserving clear progress and actions |
| **Pause / settings / dialogs** | Quiet, consistent framing, obvious keyboard focus and straightforward navigation |

## Desired interaction feel

- Opening a panel might briefly draw its borders or fade/slide elements into position, rather than simply appearing.
- Hover and keyboard focus could light a small part of a frame, reveal a subtle corner bracket or brighten a line.
- Selected states should remain unmistakable after the transition finishes.
- Disabled/blocked/unavailable states should be clear without relying solely on colour.
- Expensive or destructive actions should remain immediately understandable even when the interface is more decorative.
- Animations should be short, interruptible when practical and should not delay gameplay input.

## Technical direction to evaluate later — not yet a design decision

The repository already has **`UI/UIMasterConfig.cs`**, **`UI/DefaultUIConfig.tres`**, **`UI/Shared/UIButtonFactory.cs`** and **`UI/Shared/MenuLayout.cs`**. These provide a shared starting point but not a complete visual system for every panel.

A future plan should evaluate how to establish **one coherent visual authority** for the interface while keeping individual screens responsible for their content and behaviour. Possible approaches include a native Godot Theme, reusable panel/frame components, shared style resources, optional vector-decoration layers and additions to existing factories. **Do not select an architecture on the basis of this brainstorm alone.**

Practical considerations for that future evaluation:

- Reuse current UI systems; do not create a parallel manager or rebuild gameplay/menu logic merely for appearance.
- Make panel decorations optional and composable so compact HUD pieces are not forced to match full-screen menu compositions.
- Prefer **event-driven or bounded** animation; decorative elements should sleep when hidden or static.
- Test real blur, glow and shader effects for cost. Use cheaper approximations where full post-processing would be disproportionate.
- Preserve mouse input, keyboard focus, controller navigation where supported, tooltips, contrast, resizing and clipping.
- Consider reduced-motion and low-effects alternatives if persistent effects become distracting or costly.
- Keep gameplay-critical warnings and statistics legible over a busy battlefield.
- Retain the source-owned behaviour of each interface (tower placement, Inspector actions, Research, saving, Results, etc.).

## Future brainstorming / decisions

These are questions to answer **before** making an implementation plan:

1. What reference images or rough mockups best capture the desired vector/energy style?
2. Which corner shapes and edge effects feel distinctive without becoming visually repetitive?
3. How much glow, panel transparency and background blur is appropriate in gameplay versus menus?
4. Which movements actually convey state or progress, and which are purely decorative?
5. Should panels have a few shared visual families rather than one universal frame?
6. What should remain static, and what should animate only on selection, opening, warning or completion?
7. How should the new style scale at different resolutions without shrinking important text?
8. Which existing screen should act as the **first visual pilot**—likely the Build Menu and Inspector—before applying a style across the game?

## Scope boundary

**This file is a concept notebook, not an implementation plan.** Nothing here authorizes code changes, a new Theme, shaders, colour changes, UI restructuring, new autoloads or a wholesale interface replacement.

Future approved designs and plans may refer back to this note for creative direction, but must separately specify the chosen look, scope, ownership, performance budget, interaction behaviour and validation approach. Until then, existing UIs should continue functioning unchanged.
