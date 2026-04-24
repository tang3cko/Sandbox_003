shader_type spatial;
render_mode cull_disabled, unshaded;

#include "includes/noise.gdshaderinc"
#include "includes/color_utils.gdshaderinc"

uniform vec3 aura_color : source_color = vec3(0.8, 0.3, 1.0);
uniform float aura_intensity : hint_range(0.0, 3.0) = 1.5;
uniform float noise_speed = 1.0;
uniform float noise_scale = 3.0;
uniform float edge_softness : hint_range(0.0, 1.0) = 0.5;

void fragment() {
	vec2 uv = UV * 2.0 - 1.0;
	float dist = length(uv);

	// Radial falloff
	float alpha = 1.0 - smoothstep(0.3, 1.0, dist);

	// Animated noise pattern
	float noise = fbm(uv * noise_scale + TIME * noise_speed, 3);
	alpha *= (0.6 + 0.4 * noise);

	// Edge softness
	alpha *= smoothstep(1.0, 1.0 - edge_softness, dist);

	float p = pulse(TIME, 2.0);
	vec3 color = aura_color * aura_intensity * (0.8 + 0.2 * p);

	ALBEDO = color;
	EMISSION = color * 0.5;
	ALPHA = alpha * 0.6;
}
