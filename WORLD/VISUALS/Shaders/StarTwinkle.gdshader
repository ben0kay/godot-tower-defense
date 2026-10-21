shader_type canvas_item;
render_mode blend_mix;
// Sparse independent dots; existing baked starfield is not animated as a whole.
uniform vec4 tint : source_color = vec4(0.74, 0.86, 1.0, 1.0);
uniform float opacity : hint_range(0.0, 1.0) = 0.45;
uniform float density : hint_range(0.0, 1.0) = 0.2;
uniform float twinkle_amount : hint_range(0.0, 1.0) = 0.45;
uniform float twinkle_speed : hint_range(0.0, 5.0) = 0.65;
float rand(vec2 p) { return fract(sin(dot(p, vec2(127.1,311.7))) * 43758.5453); }
void fragment() {
    vec2 g = UV * vec2(80.0,64.0);
    vec2 cell = floor(g);
    vec2 p = fract(g) - 0.5 - (vec2(rand(cell + 3.0),rand(cell + 7.0)) - 0.5) * 0.75;
    float dot_alpha = 1.0 - smoothstep(0.007, 0.041, length(p));
    float star = step(1.0 - density, rand(cell + 17.0));
    float phase = 6.28318 * rand(cell + 71.0);
    float twinkle = 1.0 - twinkle_amount * (0.5 + 0.5 * sin(TIME * twinkle_speed + phase));
    COLOR = vec4(tint.rgb, opacity * tint.a * star * dot_alpha * twinkle);
}