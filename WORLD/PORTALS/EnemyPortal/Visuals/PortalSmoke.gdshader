// Particle phase drives billows and dissolution, so no wall-clock TIME advances smoke during pause.
shader_type canvas_item;
render_mode unshaded;
uniform vec4 smoke_tint : source_color = vec4(0.52, 0.22, 0.76, 1.0);
uniform float opacity : hint_range(0.0, 1.0) = 0.55;
varying float age;
float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
float noise(vec2 p) {
    vec2 i = floor(p), f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), f.x),
        mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0)), f.x), f.y);
}
void vertex() {
    age = clamp(INSTANCE_CUSTOM.y, 0.0, 1.0);
    VERTEX *= mix(0.65, 1.3, age);
}
void fragment() {
    vec2 p = (UV - vec2(0.5)) * 2.0;
    vec2 drift = vec2(age * 1.3, -age * 0.9);
    float billow = noise(p * 3.0 + drift) * 0.67 + noise(p * 6.0 - drift) * 0.33;
    float radius = length(p);
    float edge = 0.77 + (billow - 0.5) * 0.35;
    float shape = 1.0 - smoothstep(edge - 0.28, edge, radius);
    float dissolve = 1.0 - smoothstep(0.40, 1.0, age);
    float fade_in = smoothstep(0.0, 0.12, age);
    float rim = smoothstep(0.20, 0.85, radius) * (0.5 + billow * 0.5);
    vec3 plum = smoke_tint.rgb * (0.38 + billow * 0.45);
    vec3 lavender = min(vec3(1.0), smoke_tint.rgb + vec3(0.32, 0.19, 0.25));
    COLOR = vec4(mix(plum, lavender, rim), shape * fade_in * dissolve * opacity * smoke_tint.a);
}
