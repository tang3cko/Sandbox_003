shader_type spatial;
render_mode cull_back, unshaded;

// COLOR: projectile color from WeaponConfig

uniform float glow_intensity : hint_range(0.0, 5.0) = 2.0;
uniform float trail_stretch = 1.5;
uniform float pulse_speed = 6.0;

void vertex() {
	// Stretch along velocity direction (stored in INSTANCE_CUSTOM.rg as normalized direction)
	float dir_x = INSTANCE_CUSTOM.r;
	float dir_z = INSTANCE_CUSTOM.g;

	if (abs(dir_x) + abs(dir_z) > 0.01) {
		float stretch = 1.0 + trail_stretch;
		// Stretch along local Z (forward)
		VERTEX.z *= stretch;
	}
}

void fragment() {
	vec3 base_color = COLOR.rgb;
	float p = 0.5 + 0.5 * sin(TIME * pulse_speed);
	float brightness = glow_intensity * (0.8 + 0.2 * p);

	ALBEDO = base_color * brightness;
	EMISSION = base_color * brightness * 0.8;
}
