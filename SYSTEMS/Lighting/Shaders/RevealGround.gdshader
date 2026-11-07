shader_type canvas_item;
render_mode unshaded, blend_add;
#include "res://SYSTEMS/Lighting/Shaders/LightReveal.gdshaderinc"

uniform float lift = 0.075;

void vertex() {
    reveal_world_position = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
}

void fragment() {
    COLOR = vec4(vec3(1.0), reveal_enabled ? sample_reveal().g * lift : 0.0);
}
