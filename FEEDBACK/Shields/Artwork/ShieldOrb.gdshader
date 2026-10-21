shader_type canvas_item;
render_mode blend_mix;

// Shared, static GPU-rendered shield. Every ship uses the same small quad and shader.
// Shape is evaluated per visible pixel; no CPU geometry generation, TIME or animation loop.
uniform float rim_brightness : hint_range(0.0, 2.0, 0.05) = 1.0;
uniform float rim_thickness : hint_range(0.01, 0.30, 0.01) = 0.10;
uniform float hex_visibility : hint_range(0.0, 1.0, 0.01) = 0.20;
uniform float field_opacity : hint_range(0.0, 0.5, 0.01) = 0.045;
uniform float halo_strength : hint_range(0.0, 1.0, 0.01) = 0.14;

// Repeated hex-cell distance, with two staggered cell centres.
float hex_cell(vec2 p) {
    vec2 tile = vec2(1.73205, 1.5);
    vec2 a = mod(p, tile) - tile * 0.5;
    vec2 b = mod(p - tile * 0.5, tile) - tile * 0.5;
    vec2 q = dot(a, a) < dot(b, b) ? a : b;
    q = abs(q);
    return max(q.y, q.x * 0.8660254 + q.y * 0.5);
}

void fragment() {
    vec2 p = (UV - vec2(0.5)) * 2.0;
    float r = length(p);
    float disc = 1.0 - smoothstep(0.965, 1.0, r);
    float outer_halo = exp(-pow((r - 0.91) / 0.092, 2.0));
    float edge = exp(-pow((r - 0.875) / max(0.008, rim_thickness * 0.32), 2.0));
    float glass = exp(-pow((r - 0.76) / max(0.02, rim_thickness * 1.55), 2.0));
    float mid = 0.5 + 0.5 * p.y;
    float curved_light = exp(-pow((p.y + 0.64) / 0.13, 2.0)) * (1.0 - smoothstep(0.25, 0.83, abs(p.x)));
    float hex_edge = smoothstep(0.435, 0.477, hex_cell(p * 5.0));
    float hex_fade = smoothstep(0.20, 0.50, r) * (1.0 - smoothstep(0.83, 0.96, r));
    float facets = hex_edge * hex_fade * hex_visibility;
    float alpha = field_opacity * (0.55 + 0.45 * r) + glass * 0.17 + edge * 0.70 * rim_brightness
        + outer_halo * halo_strength + curved_light * 0.14 + facets * 0.40;
    // Multiply by vertex tint so existing C# shield colour and impact-fade work unchanged.
    COLOR = vec4(vec3(1.0), clamp(alpha * disc, 0.0, 1.0)) * COLOR;
}
