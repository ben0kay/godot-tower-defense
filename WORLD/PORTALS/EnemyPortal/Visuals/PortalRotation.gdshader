shader_type canvas_item;
render_mode unshaded;

// Only the existing artwork quad rotates. Root, label, particles, footprint and exit stay fixed.
uniform float radians_per_second : hint_range(0.0, 0.175) = 0.034906585;
void vertex() {
    float angle = TIME * radians_per_second;
    float c = cos(angle);
    float s = sin(angle);
    VERTEX = mat2(vec2(c, s), vec2(-s, c)) * VERTEX;
}
