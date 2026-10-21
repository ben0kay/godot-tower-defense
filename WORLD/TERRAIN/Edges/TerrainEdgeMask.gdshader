shader_type canvas_item;
render_mode blend_mix;
uniform sampler2D apply_cells : filter_nearest, repeat_disable;
uniform sampler2D edge_mask : filter_linear, repeat_disable;
uniform mat3 world_to_grid;
uniform vec2 mask_origin;
uniform vec2 mask_extent;
uniform float cell_size = 64.0;
varying vec2 world_point;
void vertex() { world_point = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy; }
void fragment() {
    vec2 cell = (world_to_grid * vec3(world_point, 1.0)).xy / cell_size;
    vec2 mask_uv = (cell - mask_origin) / mask_extent;
    if (texture(apply_cells, mask_uv).r > 0.5) COLOR.a *= texture(edge_mask, mask_uv).r;
}
