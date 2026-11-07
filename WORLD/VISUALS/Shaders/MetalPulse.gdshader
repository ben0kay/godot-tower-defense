// TileData material for selected industrial accents. The atlas texture owns the alpha mask.
shader_type canvas_item;

uniform bool enabled = true;
uniform vec4 accent_tint : source_color = vec4(0.45, 0.86, 0.94, 1.0);
uniform float baseline_brightness : hint_range(0.0, 1.5, 0.01) = 1.0;
uniform float pulse_strength : hint_range(0.0, 0.5, 0.01) = 0.18;
uniform float pulse_frequency : hint_range(0.01, 1.0, 0.01) = 0.16;
uniform float pulse_coverage : hint_range(0.02, 0.5, 0.01) = 0.20;
uniform float phase_variation : hint_range(0.0, 0.01, 0.0001) = 0.0014;

varying vec2 world_position;

void vertex() {
    world_position = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
}

void fragment() {
    if (enabled) {
        // A world-space sweep stays continuous across connected cells and never offsets atlas UVs.
        float phase = fract(TIME * pulse_frequency - dot(world_position, vec2(1.0, 0.6)) * phase_variation);
        float band = 1.0 - smoothstep(0.0, pulse_coverage, abs(phase - 0.5));
        float intensity = max(COLOR.r, max(COLOR.g, COLOR.b));
        COLOR.rgb = COLOR.rgb * baseline_brightness + accent_tint.rgb * intensity * band * pulse_strength;
    }
}
