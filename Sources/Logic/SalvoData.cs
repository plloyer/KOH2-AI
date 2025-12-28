namespace Logic;

public class SalvoData
{
	public class Def : Logic.Def
	{
		public string arrow_shoot_sound_effect;

		public string arrow_impact_sound_effect;

		public string reload_sound_effect;

		public string release_sound_effect;

		public float reload_sound_delay = -1f;

		public bool splash_damage;

		public float explosion_force = -1f;

		public float projectile_radius = 0.5f;

		public float shoot_height = 0.5f;

		public float shoot_offset;

		public float min_shoot_range = 25f;

		public float max_shoot_range = 60f;

		public float min_shoot_angle = -60f;

		public float max_shoot_angle = 60f;

		public float min_shoot_speed = 20f;

		public float shoot_speed_randomization_mod = 0.1f;

		public float collision_check_offset = -0.5f;

		public float max_end_position_offset = 0.5f;

		public float gravity = 10f;

		public float friendly_fire_mod = 0.5f;

		public bool can_hit_fortification;

		public int arrows_per_troop = 1;

		public float height_randomized_offset;

		public float width_randomized_offset;

		public float random_shoot_time_offset = 0.5f;

		public bool draw_after_landing = true;

		public int troops_def_idx = -1;

		public override bool Load(Game game)
		{
			arrow_shoot_sound_effect = base.field.GetString("arrow_shoot_sound_effect", null, arrow_shoot_sound_effect);
			arrow_impact_sound_effect = base.field.GetString("arrow_impact_sound_effect", null, arrow_impact_sound_effect);
			reload_sound_delay = base.field.GetFloat("reload_sound_delay", null, reload_sound_delay);
			reload_sound_effect = base.field.GetString("reload_sound_effect", null, reload_sound_effect);
			release_sound_effect = base.field.GetString("release_sound_effect", null, release_sound_effect);
			splash_damage = base.field.GetBool("splash_damage", null, splash_damage);
			explosion_force = base.field.GetFloat("explosion_force", null, explosion_force);
			projectile_radius = base.field.GetFloat("projectile_radius", null, projectile_radius);
			shoot_height = base.field.GetFloat("shoot_height", null, shoot_height);
			shoot_offset = base.field.GetFloat("shoot_offset", null, shoot_offset);
			min_shoot_range = base.field.GetFloat("min_shoot_range", null, min_shoot_range);
			max_shoot_range = base.field.GetFloat("max_shoot_range", null, max_shoot_range);
			min_shoot_angle = base.field.GetFloat("min_shoot_angle", null, min_shoot_angle);
			max_shoot_angle = base.field.GetFloat("max_shoot_angle", null, max_shoot_angle);
			min_shoot_speed = base.field.GetFloat("min_shoot_speed", null, min_shoot_speed);
			shoot_speed_randomization_mod = base.field.GetFloat("shoot_speed_randomization_mod", null, shoot_speed_randomization_mod);
			collision_check_offset = base.field.GetFloat("collision_check_offset", null, collision_check_offset);
			max_end_position_offset = base.field.GetFloat("max_end_position_offset", null, max_end_position_offset);
			gravity = base.field.GetFloat("gravity", null, gravity);
			friendly_fire_mod = base.field.GetFloat("friendly_fire_mod", null, friendly_fire_mod);
			can_hit_fortification = base.field.GetBool("can_hit_fortification", null, can_hit_fortification);
			arrows_per_troop = base.field.GetInt("arrows_per_troop", null, arrows_per_troop);
			height_randomized_offset = base.field.GetFloat("height_randomized_offset", null, height_randomized_offset);
			width_randomized_offset = base.field.GetFloat("width_randomized_offset", null, width_randomized_offset);
			draw_after_landing = base.field.GetBool("draw_after_landing", null, draw_after_landing);
			random_shoot_time_offset = base.field.GetFloat("random_shoot_time_offset", null, random_shoot_time_offset);
			return base.Load(game);
		}
	}
}
