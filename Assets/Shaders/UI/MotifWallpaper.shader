Shader "FirstJobber/UI/MotifWallpaper"
{
    Properties
    {
        [PerRendererData] _MainTex ("Motif Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _SpriteUVRect ("Sprite UV Rectangle", Vector) = (0,0,1,1)
        _RectSize ("Viewport Size", Vector) = (1920,1080,0,0)
        _CellSize ("Repeat Size", Vector) = (960,640,0,0)
        _ElementSize ("Motif Size", Vector) = (300,300,0,0)
        _MotifOffset ("Motif Offset", Vector) = (0,0,0,0)
        _ScrollOffset ("Scroll Offset", Vector) = (0,0,0,0)
        _MotifRotation ("Motif Rotation (cos, sin)", Vector) = (1,0,0,0)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            Name "MotifWallpaper"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 localPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            sampler2D _MainTex;
            fixed4 _Color;
            float4 _SpriteUVRect, _RectSize, _CellSize, _ElementSize, _MotifOffset, _ScrollOffset, _ClipRect;
            float4 _MotifRotation;

            v2f vert(appdata_t input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.localPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;
                output.color = input.color * _Color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                // The discontinuity lies in transparent spacing, not at an artwork edge.
                float2 pixel = (input.texcoord - 0.5) * _RectSize.xy - _ScrollOffset.xy - _MotifOffset.xy;
                float2 cell = frac(pixel / _CellSize.xy + 0.5) - 0.5;
                float2 motifPixel = cell * _CellSize.xy;
                // Inverse rotation samples the original art while visually tilting it about its centre.
                float2 rotatedPixel = float2(
                    _MotifRotation.x * motifPixel.x + _MotifRotation.y * motifPixel.y,
                    -_MotifRotation.y * motifPixel.x + _MotifRotation.x * motifPixel.y);
                float2 motifUV = rotatedPixel / _ElementSize.xy + 0.5;
                clip(min(min(motifUV.x, motifUV.y), min(1.0 - motifUV.x, 1.0 - motifUV.y)));
                float2 textureUV = _SpriteUVRect.xy + motifUV * _SpriteUVRect.zw;
                fixed4 color = tex2D(_MainTex, textureUV) * input.color;
                #ifdef UNITY_UI_CLIP_RECT
                    color.a *= UnityGet2DClipping(input.localPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                    clip(color.a - 0.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
