// Smooth procedural portal smoke. Particle age drives motion/fades, so paused gameplay freezes the cloud.
// Rotated, softly warped noise prevents square-looking patches; the quad is fully transparent at its border.
shader_type canvas_item;
render_mode unshaded;
#include "res://SYSTEMS/Lighting/Shaders/LightReveal.gdshaderinc"

uniform vec4 smoke_tint : source_color = vec4(0.52, 0.22, 0.76, 1.0);
uniform float opacity : hint_range(0.0, 1.0) = 0.55;

varying float age;
varying float cloud_seed;

float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }

// Quintic interpolation gives each noise cell a smooth curvature, without hard, boxy transitions.
float noise(vec2 p) {
    vec2 i = floor(p), f = fract(p);
    f = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
    return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), f.x),
        mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), f.x), f.y);
}

void vertex() {
    cloud_seed = INSTANCE_CUSTOM.x * 3.7;
    age = clamp(INSTANCE_CUSTOM.y, 0.0, 1.0);
    VERTEX *= mix(0.6, 2.0, age);
    reveal_world_position = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
}

void fragment() {
    vec2 p = (UV - vec2(0.5)) * 2.0;
    vec2 drift = vec2(age * 2.2 + cloud_seed, -age * 1.5 - cloud_seed);

    // Rotate the noise coordinates and bend them gently so lattice axes never read as squares.
    vec2 rotated = vec2(p.x * 0.866 - p.y * 0.5, p.x * 0.5 + p.y * 0.866);
    vec2 bent = rotated + vec2(
        sin(rotated.y * 3.1 + drift.x * 0.45),
        sin(rotated.x * 2.7 - drift.y * 0.45)) * 0.16;
    float billow = noise(bent * 2.7 + drift * 0.65) * 0.64
        + noise(bent * 5.3 - drift * 0.43) * 0.36;

    float radius = length(p);
    float wavy_radius = radius + (billow - 0.5) * 0.16;
    // A wide falloff turns each puff into translucent gas rather than a cut-out cloud silhouette.
    float body = 1.0 - smoothstep(0.30, 0.94, wavy_radius);

    // Safety feather on EVERY side of the particle quad. This guarantees no clipped square edges,
    // including when puffs are enlarged, rotated, or overlap the softer large-cloud layer.
    float to_border = min(min(UV.x, 1.0 - UV.x), min(UV.y, 1.0 - UV.y));
    float border_fade = smoothstep(0.0, 0.16, to_border);
    float density = mix(0.64, 1.0, billow);

    float fade_in = smoothstep(0.0, 0.12, age);
    float dissolve = 1.0 - smoothstep(0.65, 1.0, age);
    float rim = smoothstep(0.20, 0.85, radius) * (0.5 + billow * 0.5);
    vec3 plum = smoke_tint.rgb * (0.38 + billow * 0.45);
    vec3 lavender = min(vec3(1.0), smoke_tint.rgb + vec3(0.32, 0.19, 0.25));
    COLOR = vec4(mix(plum, lavender, rim),
        body * border_fade * density * fade_in * dissolve * opacity * smoke_tint.a * (reveal_enabled ? sample_reveal().r : 1.0));
}
