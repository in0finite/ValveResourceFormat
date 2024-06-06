#version 460

layout (early_fragment_tests) in;

#define F_ALPHA_TEST 0

#if (F_ALPHA_TEST == 1)
    layout (location = 0) in vec2 texCoord;
    uniform sampler2D g_tColor;
    uniform float g_flAlphaTestReference = 0.5;
#endif

void main()
{
    #if (F_ALPHA_TEST == 1)
        float opacity = texture(g_tColor, texCoord).a;
        if (opacity - 0.001 < g_flAlphaTestReference)
            discard;
    #endif
}
