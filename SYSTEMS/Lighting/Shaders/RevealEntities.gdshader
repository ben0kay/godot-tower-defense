shader_type canvas_item;
#include "res://SYSTEMS/Lighting/Shaders/LightReveal.gdshaderinc"

void vertex() {
    reveal_world_position = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
}

void fragment() {
    if (reveal_enabled) {
        COLOR.a *= sample_reveal();
    }
}
