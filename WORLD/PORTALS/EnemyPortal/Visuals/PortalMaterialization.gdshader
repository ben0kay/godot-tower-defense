shader_type canvas_item;
render_mode unshaded;
// All one-shot motion derives from pausable tween progress, never global TIME or screen reads.
uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float intensity = 1.0;
uniform float ring_radius = 46.0;
uniform float flash_strength = 1.0;
uniform bool reduced_motion = false;
void fragment() {
    vec2 pos = (UV - vec2(0.5)) * 224.0;
    float r = length(pos);
    float angle = atan(pos.y, pos.x);
    float p = clamp(progress, 0.0, 1.0);
    float grow = smoothstep(0.03, 0.60, p);
    float fade = smoothstep(0.0, 0.09, p) * (1.0 - smoothstep(0.83, 1.0, p));
    float radius = mix(3.0, ring_radius, grow);
    float motion = reduced_motion ? 0.0 : p * 19.0;
    float ring = exp(-abs(r - radius) * 0.85);
    float broken = 0.45 + 0.55 * pow(0.5 + 0.5 * sin(angle * 7.0 - motion), 3.0);
    float spiral = pow(0.5 + 0.5 * sin(angle * 3.0 + r * 0.19 + motion), 14.0);
    float spiral2 = pow(0.5 + 0.5 * sin(angle * 5.0 - r * 0.13 - motion * 0.7), 22.0);
    float disk = (1.0 - smoothstep(radius, radius + 20.0, r));
    float wisps = reduced_motion ? 0.0 : (spiral + spiral2 * 0.5) * disk * smoothstep(0.10, 0.32, p);
    float seed = exp(-r * r / (12.0 + grow * 150.0));
    float flash = exp(-pow((p - 0.78) / 0.045, 2.0)) * flash_strength;
    if (reduced_motion) flash = 0.0;
    float ripple_r = ring_radius + max(0.0, p - 0.75) * 190.0;
    float ripple = exp(-abs(r - ripple_r) * 0.45) * smoothstep(0.74, 0.79, p) * (1.0 - smoothstep(0.80, 0.98, p));
    vec3 violet = mix(vec3(0.25, 0.025, 0.65), vec3(0.85, 0.48, 1.0), clamp(wisps + ring, 0.0, 1.0));
    vec3 rgb = violet + vec3(0.8, 0.7, 0.9) * (seed * 0.4 + flash);
    float alpha = (ring * broken + wisps * 0.75 + disk * 0.16 + seed * 0.9 + flash * disk + ripple * 0.65) * fade * intensity;
    COLOR = vec4(rgb, clamp(alpha, 0.0, 1.0));
}
