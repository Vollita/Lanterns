Shader "Custom/SH_GroundSplat_BuiltIn"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Diffuse ("Diffuse", 2D) = "white" {}
        _Normal ("Normal", 2D) = "white" {}
        _Mask ("Mask", 2D) = "white" {}
        _Contrast("Contrast", Float) = 1
        _Scale("Scale", Float) = 2.65
        _Radius("Radius", Float) = 2.75
        _Increase("Increase", Float) = 3

    }
    SubShader
    {
        //Tags { "RenderType"="Transparent" "Queue" = "Transparent" }
        Tags { "RenderType"="Opaque"}
        LOD 200
        Cull Back


        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        //#pragma surface surf Standard alpha:blend fullforwardshadows addshadow
        #pragma surface surf Standard fullforwardshadows addshadow

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _Diffuse;
        sampler2D _Normal;
        sampler2D _Mask;

        float _Contrast;
        float _Scale;
        float _Radius;
        float _Increase;

        struct Input
        {
            float2 uv_Diffuse;
        };


        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.uv_Diffuse.xy;
            uv -= float2(0.5, 0.5);
            uv *= _Scale;
            uv += float2(0.5, 0.5);

            float4 diffuse = tex2D(_Diffuse, uv);
            float3 normal = UnpackNormal(tex2D(_Normal, uv));
            float4 mask = tex2D(_Mask, uv);

            o.Albedo = diffuse;
            o.Normal = normal;

            //Circle
            float circle = 1 - distance(IN.uv_Diffuse.xy, float2(0.5,0.5));
            circle = saturate(pow(circle, _Radius) * _Increase);

            float heightBlend = saturate((mask.b - 1) + (circle * 2));

            float finalHeight = saturate(lerp( (0 - _Contrast), (_Contrast + 1), heightBlend));

            o.Alpha = finalHeight;

            clip(finalHeight - 0.5);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
