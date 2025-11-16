Shader "PWS/MB_Details/PW_MB_Foliage_BuiltIn"

{
    Properties
    {
        [Header(PBR)]
        _MipBias ("Mip Bias", Range(-8,8)) = 0
        [HDR]_BaseColor ("Base Color", Color) = (1,1,1,1)
        [NoScaleOffset]_BaseMap ("Base Map (RGB)", 2D) = "white" {}
        [NoScaleOffset]_NormalMap ("Normal Map", 2D) = "white" {}
        _NormalStrength("Normal Strength", Range(0.01, 3)) = 1
        [NoScaleOffset]_MaskMap ("PBR Mask Map", 2D) = "white" {}
        _MaskMapMod("Mask Map modifiers", Vector) = (1,1,1,1)
        _Thickness("Thickness", Range(0, 1)) = 0
        _Billboard("Billboard", Float) = 0
        _Ambient("Ambient", Range(0, 1)) = 0.5
        [Space]
        

        [Header(Alpha)]
        _Cutoff ("Alpha Cut Off", Range(0,1)) = 0.05
        [Space]
        
        //wind
        [Header(Wind Options)]
        [Toggle(_PW_SF_WIND_ON)]        
        _PW_SF_WIND("Enable Wind", Int) = 1
        _PW_WindTreeFlex(" Wind Detail Flex", Vector) = (0.8,1.15,0.1,0)
        _PW_WindTreeFrequency (" Wind Detail Frequency", Vector) = (0.25,0.5,1.3,0)
        _PW_WindTreeWidthHeight (" Wind Detail Height", Vector) = (4,8,0.66,0.66)
        
    }
    SubShader
    {
        Tags { "RenderType"="Opaque"  "Queue" = "AlphaTest"}
        LOD 200
        Cull off
        
        CGPROGRAM
        #pragma shader_feature_local _PW_SF_WIND_ON
        
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Gaia fullforwardshadows vertex:vert keepalpha addshadow

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.5


        #include "UnityCG.cginc"
		#include "UnityPBSLighting.cginc"
		#include "/MB_GeneralVars.cginc"
		#include "/MB_GeneralFuncs.cginc"
        #include "/MB_GeneralWind.cginc"


        struct Input
        {
            float2 uv_BaseMap;
            float2 uv_MainTex;
            float facing: VFACE;
            float3 viewDir;
            float4 screenPos;
            float4 screenPosition;
            float3 Normal;
            float3 worldPos;
            float3 worldNormal; 
            INTERNAL_DATA
        };

        sampler2D _BaseMap, _NormalMap, _MaskMap;
        half4 _BaseColor, _ColorA, _ColorB;
        //half _Cutoff;
        float4 _MaskMapMod;
        float _Thickness,_MipBias;
        float _NormalStrength;
        float _Billboard;
        float _Ambient;


        #include "UnityShaderVariables.cginc"
        #include "UnityStandardConfig.cginc"
        #include "UnityLightingCommon.cginc"
        #include "UnityGBuffer.cginc"
        #include "UnityGlobalIllumination.cginc"

        //-------------------------------------------------------------------------------------
		half4 LightingGaia ( SurfaceOutputGaia g, half3 viewDir, UnityGI gi)
		{
			half4 addLight = 0;

            SurfaceOutputStandard s;
            s.Albedo 		= g.Albedo;
            s.Normal 		= g.Normal;
            s.Emission 		= g.Emission;
            s.Metallic 		= g.Metallic;
            s.Smoothness 	= g.Smoothness;
            s.Occlusion 	= g.Occlusion;
            s.Alpha 		= g.Alpha;


			AddLighting_half ( g.Albedo, 
						 g.e.worldNormal,
						 gi.light.dir, 
						 gi.light.color, 
						 viewDir,
						 g.e.coverRGBA, 
						 g.e.thickness,
						 float3(0,0,0),
						 0.0f,
						 0.0f,
						 1.0f,
     				     addLight
						 );



			return LightingStandard ( s, viewDir, gi ) + addLight;
        }
        
    	//-------------------------------------------------------------------------------------
        
        inline void LightingGaia_GI ( SurfaceOutputGaia s, UnityGIInput data, inout UnityGI gi )
        {
            UNITY_GI ( gi, s, data );
        }
        

        // Vertex "shader"
        void vert (inout appdata_full v) {
           
           WindCalculations_float(v.vertex.xyz,_PW_WindTreeWidthHeight.xy,_PW_WindTreeFlex,_PW_WindTreeFrequency,v.vertex.xyz);
        }
        
        // Fragment "shader" 
        void surf (Input IN, inout SurfaceOutputGaia o)
        {
            // Albedo & Alpha test
            float4 uvMipBias = float4(IN.uv_BaseMap.xy,0,_MipBias);
            fixed4 base = tex2Dbias(_BaseMap, uvMipBias);
            clip(base.a - _Cutoff);

            // PBR masks
            base *= _BaseColor * float4(0.5,0.5,0.5,1);

            float4 mask = saturate(tex2Dbias (_MaskMap, uvMipBias) * _MaskMapMod);

            // Output surface struct
            o.Albedo = base.rgb;
            o.Metallic = mask.r;
            o.Occlusion = mask.g;
            o.Smoothness = mask.a * 0.25;
            o.Alpha = base.a;

            o.Normal = UnpackNormal(tex2D(_NormalMap, uvMipBias));
            o.Normal = normalize(o.Normal);
            o.Normal = abs(o.Normal);
            //o.e.worldNormal = WorldNormalVector ( IN, o.Normal );
            o.e.thickness = 1.0 - mask.b;
            o.e.vertexColor = float4(0,0,0,0);

            float alpha = base.a - _Cutoff;

            clip( base.a - _Cutoff ); //Dither from Alpha

            
        }
        ENDCG
    }
    FallBack "Diffuse"
}
