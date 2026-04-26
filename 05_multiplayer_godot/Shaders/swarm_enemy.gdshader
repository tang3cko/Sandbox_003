shader_type spatial;
render_mode cull_back;

#include "includes/noise.gdshaderinc"
#include "includes/color_utils.gdshaderinc"

// INSTANCE_CUSTOM: R = damageFlash, G = deathProgress, B = typeIndex, A = unused
// COLOR: base color from EnemyTypeConfig

uniform float dissolve_edge_width : hint_range(0.0, 0.2) = 0.05;
uniform vec3 dissolve_edge_color : source_color = vec3(1.0, 0.3, 0.0);
uniform float rim_power : hint_range(1.0, 8.0) = 3.0;
uniform vec3 rim_color : source_color = vec3(0.5, 0.5, 0.6);
uniform float noise_scale = 4.0;

varying vec3 world_pos;
varying float v_flash;
varying float v_death;

void vertex() {
	v_flash = INSTANCE_CUSTOM.r;
	v_death = INSTANCE_CUSTOM.g;

	// Shrink and sink dying enemies
	if (v_death > 0.0) {
		float scale = 1.0 - v_death;
		VERTEX *= scale;
		VERTEX.y -= v_death * 0.5;
	}

	world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
	vec3 base_color = COLOR.rgb;

	// Damage flash: lerp toward white
	vec3 color = apply_damage_flash(base_color, v_flash);

	// Rim lighting for visibility
	vec3 rim = fresnel_rim(NORMAL, VIEW, rim_power, rim_color);
	color += rim;

	// Dissolve effect for dying enemies
	if (v_death > 0.0) {
		float noise = value_noise(world_pos.xz * noise_scale);
		float dissolve_threshold = v_death;

		if (noise < dissolve_threshold - dissolve_edge_width) {
			discard;
		}

		// Glowing edge
		if (noise < dissolve_threshold) {
			float edge_factor = 1.0 - (dissolve_threshold - noise) / dissolve_edge_width;
			color = mix(color, dissolve_edge_color, edge_factor);
			EMISSION = dissolve_edge_color * edge_factor * 2.0;
		}
	}

	ALBEDO = color;
}
