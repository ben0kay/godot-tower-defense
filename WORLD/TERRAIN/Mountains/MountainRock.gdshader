// Static mineral grain and weathered strata over cached facet lighting; no time-driven work.
shader_type canvas_item;
render_mode unshaded;

uniform float detail_strength : hint_range(0.0, 0.35) = 0.14;

float grain(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

float stone_noise(vec2 p) {
    vec2 cell = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    return mix(mix(grain(cell), grain(cell + vec2(1.0, 0.0)), f.x),
        mix(grain(cell + vec2(0.0, 1.0)), grain(cell + vec2(1.0)), f.x), f.y);
}

void fragment() {
    // The same CanvasItem records a translucent shadow, which retains its original color.
    if (COLOR.a > 0.99) {
        float broad = stone_noise(UV * 5.0);
        float fine = stone_noise(UV * 95.0);
        float strata = sin(UV.y * 52.0 + UV.x * 7.0 + broad * 6.0);
        float variation = (broad - 0.5) * 0.65 + (fine - 0.5) * 0.65 + strata * 0.15;
        COLOR.rgb *= 1.0 + variation * detail_strength;
    }
}
