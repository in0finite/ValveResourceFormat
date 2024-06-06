#version 460

// Baked lighting
#define D_BAKED_LIGHTING_FROM_LIGHTMAP 0
#define D_BAKED_LIGHTING_FROM_VERTEX_STREAM 0
#define D_BAKED_LIGHTING_FROM_PROBE 0
#define LightmapVersionNumber 0
#define LightmapGameVersionNumber 0

//#define F_DO_NOT_CAST_SHADOWS 0
uniform int F_RENDER_BACKFACES;
uniform int F_DONT_FLIP_BACKFACE_NORMALS; // New in CS2
//#define F_DISABLE_Z_BUFFERING 0
//#define F_DISABLE_Z_PREPASS 0
