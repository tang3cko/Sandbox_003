shader_type spatial;

#include "includes/noise.gdshaderinc"
#include "includes/color_utils.gdshaderinc"

uniform vec3 grid_color : source_color = vec3(0.15, 0.2, 0.3);
uniform vec3 base_color : source_color = vec3(0.05, 0.07, 0.1);
uniform vec3 danger_color : source_color = vec3(0.5, 0.1, 0.1);
uniform float grid_size = 2.0;
uniform float grid_thickness : hint_range(0.0, 0.1) = 0.03;
uniform float arena_half_size = 25.0;
uniform float danger_zone_width = 3.0;
uniform float scroll_speed = 0.3;

void fragment() {
	vec2 uv = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xz;

	// Scrolling subtle noise
	float noise = fbm(uv * 0.5 + TIME * scroll_speed * vec2(0.3, 0.1), 3) * 0.1;

	// Grid lines
	vec2 grid_uv = mod(uv, grid_size) / grid_size;
	float grid_x = step(grid_uv.x, grid_thickness) + step(1.0 - grid_thickness, grid_uv.x);
	float grid_z = step(grid_uv.y, grid_thickness) + step(1.0 - grid_thickness, grid_uv.y);
	float grid = clamp(grid_x + grid_z, 0.0, 1.0);

	vec3 color = mix(base_color, grid_color, grid * 0.5);
	color += noise;

	// Danger zone at arena edges
	float dist_to_edge = arena_half_size - max(abs(uv.x), abs(uv.y));
	if (dist_to_edge < danger_zone_width) {
		float danger = 1.0 - dist_to_edge / danger_zone_width;
		float danger_pulse = pulse(TIME, 2.0);
		color = mix(color, danger_color, danger * 0.3 * (0.5 + 0.5 * danger_pulse));
	}

	ALBEDO = color;
}
