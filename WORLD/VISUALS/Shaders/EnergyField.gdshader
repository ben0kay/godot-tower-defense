shader_type canvas_item;
render_mode blend_mix;

// Shared VE2 energy overlay: portal whirl (mode 0) or Citadel reactor (mode 1).
// Fragment-only TIME animation; no CPU updates while idle.
uniform int field_mode = 0;
uniform vec4 energy_tint : source_color = vec4(0.55, 0.32, 0.98, 1.0);
uniform float idle_opacity : hint_range(0.0, 1.0) = 0.48;
uniform float animation_speed : hint_range(0.0, 3.0) = 0.35;
uniform float glow_width : hint_range(0.01, 0.5) = 0.12;

void fragment() {
    vec2 p = (UV - vec2(0.5)) * 2.0;
    float radius = length(p);
    float theta = atan(p.y, p.x);
    float t = TIME * animation_speed;
    float core = (1.0 - smoothstep(0.10, 0.72, radius));
    float ring = exp(-pow((radius - 0.81) / max(glow_width, 0.02), 2.0));
    float alpha = 0.0;
    if (field_mode == 0) {
        // Softly twisting purple filaments inside the portal, no hard segment ends.
        float swirl = sin(theta * 5.0 - radius * 17.0 + t * 2.0);
        float filaments = pow(0.5 + 0.5 * swirl, 5.0);
        float inner_mask = smoothstep(0.12, 0.25, radius) * (1.0 - smoothstep(0.69, 0.86, radius));
        float breath = 0.86 + 0.14 * sin(t * 1.8);
        alpha = (filaments * inner_mask * 0.30 + ring * 0.24 + core * 0.10) * breath;
    } else {
        // Quiet teal reactor breathing with diffuse rather than full-screen glow.
        float breath = 0.82 + 0.18 * sin(t * 1.45);
        float reactor = exp(-pow(radius / 0.42, 2.0));
        float inner_ring = exp(-pow((radius - 0.42) / max(glow_width, 0.02), 2.0));
        alpha = (reactor * 0.42 + inner_ring * 0.18) * breath;
    }
    // Transparent quad corners, independent of the backdrop; no screen read or bloom.
    alpha *= 1.0 - smoothstep(0.93, 1.0, radius);
    COLOR = vec4(energy_tint.rgb, clamp(alpha * idle_opacity * energy_tint.a, 0.0, 1.0));
}
