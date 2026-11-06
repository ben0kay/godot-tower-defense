// Per-portal shader spin: smooth continuous idle rotation plus integrated, fading spawn acceleration.
// Reanchored only on accepted spawn bursts; never moves portal gameplay transforms or needs CPU updates.
shader_type canvas_item;
render_mode unshaded;

uniform float radians_per_second : hint_range(0.0, 9.424778) = 3.1415927;
uniform float anchor_time = 0.0;
uniform float anchor_angle = 0.0;
uniform float boost_radians_per_second : hint_range(0.0, 8.37758) = 0.0;
uniform float boost_duration : hint_range(0.1, 2.0) = 0.7;
uniform float noise_offset = 0.0;

void vertex() {
    // TIME normally wraps at 3600s in Godot. Maintain phase even if a burst crossed that boundary.
    float elapsed = TIME - anchor_time;
    if (elapsed < 0.0) elapsed += 3600.0;
    float duration = max(boost_duration, 0.001);
    float t = min(elapsed, duration);
    // Integral of a linearly decaying positive speed surge: extra angle remains after the surge.
    float surge = boost_radians_per_second * (t - t * t / (2.0 * duration));
    float wobble = sin(TIME * 2.1 + noise_offset) * 0.045 +
        sin(TIME * 4.7 + noise_offset * 1.7) * 0.018;
    float angle = anchor_angle + elapsed * radians_per_second + surge + wobble;
    float c = cos(angle);
    float s = sin(angle);
    VERTEX = mat2(vec2(c, s), vec2(-s, c)) * VERTEX;
}
