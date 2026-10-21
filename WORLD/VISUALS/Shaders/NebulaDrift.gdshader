shader_type canvas_item;
render_mode blend_mix;
// VE1 organic cloud wisps: warped multi-scale noise and a radial/island opacity mask.
// No rectangular UV-edge fade, CPU frame processing or separate per-cloud scene nodes.
uniform vec4 tint : source_color = vec4(0.22, 0.38, 0.64, 1.0);
uniform float opacity : hint_range(0.0, 1.0) = 0.12;
uniform float drift_speed : hint_range(0.0, 0.1) = 0.002;

float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
float noise2(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), f.x),
               mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), f.x), f.y);
}
float fbm(vec2 p) {
    float a = 0.54;
    float value = 0.0;
    for (int i = 0; i < 4; i++) {
        value += noise2(p) * a;
        p = p * 2.03 + vec2(7.31, 3.17);
        a *= 0.48;
    }
    return value;
}
void fragment() {
    vec2 drift = vec2(TIME * drift_speed, TIME * drift_speed * 0.31);
    vec2 uv = UV;
    vec2 base = uv * vec2(3.25, 2.6) + drift;
    vec2 warp = vec2(fbm(base * 0.78 + vec2(1.8, 7.1)),
                     fbm(base * 0.78 + vec2(8.4, 2.9))) - vec2(0.5);
    vec2 field_uv = base + warp * 0.95;
    float cloud = fbm(field_uv);
    float wisps = smoothstep(0.47, 0.71, cloud);

    // Organic ellipse islands taper smoothly before approaching any quad boundary.
    // Unlike a rectangular feather, this cannot expose straight horizontal or vertical fade bands.
    vec2 elliptical = (uv - vec2(0.5)) * vec2(1.45, 1.7);
    float irregularity = (fbm(base * 0.58 + vec2(19.4, 4.1)) - 0.5) * 0.22;
    float radius = length(elliptical) + irregularity;
    float natural_falloff = 1.0 - smoothstep(0.46, 0.92, radius);
    float alpha = wisps * natural_falloff * opacity * tint.a;
    COLOR = vec4(tint.rgb, clamp(alpha, 0.0, 1.0));
}
