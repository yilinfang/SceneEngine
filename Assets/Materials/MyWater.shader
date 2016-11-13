Shader "Water/MyWater" {
	Properties {
		_WaterTex ("Water", 2D) = "black" {} 
		_WaveTex ("Wave", 2D) = "black" {}
		_BumpTex ("Bump", 2D) = "bump" {} 
		_GTex ("Gradient", 2D) = "white" {}
		_NoiseTex ("Noise", 2D) = "white" {}

		_WaveSpeed ("Wave Speed", float) = 30
		_WaveLength("Wave Length", float) = 5
		_WaveAmplitude("Wave Amplitude", float) = 0.1

		_BasicColorDepth("Basic Color Smooth Depth", float) = 10
		_WaveNoiseDepth("Wave & Noise Smooth Depth (Surface)", float) = 5
		_WaveShockRange ("Wave Shock Range (Surface)", float) = 0.5
		_WaveNoiseRange ("Wave Noise Range (Surface)", float) = 6
		_WaveInterval ("Wave Interval (Surface)", float) = 2.5
		_SurfaceSpeed ("Surface Speed", float) = 2

		_Refract ("Surface Refract", float) = 0.1
		_Specular ("Surface Specular", float) = 2
		_Gloss ("Surface Gloss", float) = 1
		_SpecColor ("Surface SpecColor", color) = (1, 1, 1, 1)

		_Region("Region (x1,x2,y1,y2)", Vector) = (0,1,0,1)
		_VertexHeight("Vertex Height", Vector) = (0,0,0,0)
	}
	CGINCLUDE
	fixed4 LightingBlinn(SurfaceOutput s, fixed3 lightDir, half3 viewDir, fixed atten) {
	    half3 halfVector = normalize(lightDir + viewDir);
		float diffFactor = max(0, dot(lightDir, s.Normal)) * 0.8 + 0.2;
		float nh = max(0, dot(halfVector, s.Normal));
		float spec = pow(nh, s.Specular * 128.0) * s.Gloss;
		fixed4 c;
		c.rgb = (s.Albedo * _LightColor0.rgb * diffFactor + _SpecColor.rgb * spec * _LightColor0.rgb) * (atten * 2);
		c.a = s.Alpha + spec * _SpecColor.a * diffFactor * spec;
		return c;
	}
	ENDCG
	SubShader {
		Tags { "RenderType"="Transparent" "Queue"="Transparent"}
		LOD 200

		zwrite off

		CGPROGRAM
		#pragma surface surf Blinn vertex:vert alpha
		#pragma target 3.0

		sampler2D _CameraDepthTexture;
		sampler2D _GTex;
		sampler2D _WaterTex;
		sampler2D _BumpTex;
		sampler2D _NoiseTex;
		sampler2D _WaveTex;

		half _WaveSpeed;
		float _WaveLength;
		float _WaveAmplitude;

		float _BasicColorDepth;
		float _WaveNoiseDepth;
		half _WaveShockRange;
		half _WaveNoiseRange;
		half _SurfaceSpeed;
		fixed _WaveInterval;

		fixed _Refract;
		half _Specular;
		fixed _Gloss;

		float4 _Region;
		float4 _VertexHeight;

		struct Input {
			float2 uv_WaterTex;
			float2 uv_NoiseTex;
			float4 proj;
			float3 viewDir;
			float vertColor;
		};
		void vert (inout appdata_full v, out Input i) {
		    UNITY_INITIALIZE_OUTPUT(Input, i);

		    float3 wp = mul(unity_ObjectToWorld,(v.vertex)).xyz;

			float xt = _Time * _WaveSpeed + wp.x;
			float yt = _Time * _WaveSpeed + wp.z;

            float final = (
                sin((xt - (int)(xt/_WaveLength)*_WaveLength)*(6.283185/_WaveLength))
                + sin((yt - (int)(yt/_WaveLength)*_WaveLength)*(6.283185/_WaveLength))
            ) * _WaveAmplitude;

		    final += (
		        _VertexHeight.x * (_Region.y - wp.x) * (_Region.w - wp.z)
                + _VertexHeight.y * (wp.x - _Region.x) * (_Region.w - wp.z)
                + _VertexHeight.z * (_Region.y - wp.x) * (wp.z - _Region.z)
                + _VertexHeight.w * (wp.x - _Region.x) * (wp.z - _Region.z)
            ) / ((_Region.y-_Region.x)*(_Region.w-_Region.z));
			
			v.vertex.xyz= float3(v.vertex.x , v.vertex.y + final, v.vertex.z);
            v.normal = normalize(float3( v.normal.x + final, v.normal.y, v.normal.z));

			i.proj = ComputeScreenPos(mul(UNITY_MATRIX_MVP, v.vertex));
			COMPUTE_EYEDEPTH(i.proj.z);
		}

		void surf (Input IN, inout SurfaceOutput o) {
			float4 offsetColor = (
			    tex2D(_BumpTex, IN.uv_WaterTex + float2(_SurfaceSpeed*_Time.x,0))
			    + tex2D(_BumpTex, float2(1-IN.uv_WaterTex.y,IN.uv_WaterTex.x) + float2(_SurfaceSpeed*_Time.x,0))
			)/2;
			half2 offset = UnpackNormal(offsetColor).xy * _Refract;
			float4 bumpColor = (
			    tex2D(_BumpTex, IN.uv_WaterTex + offset + float2(_SurfaceSpeed*_Time.x,0))
			    + tex2D(_BumpTex, float2(1-IN.uv_WaterTex.y,IN.uv_WaterTex.x)+offset + float2(_SurfaceSpeed*_Time.x,0))
			)/2;
		    half m_depth = LinearEyeDepth(tex2Dproj (_CameraDepthTexture, IN.proj).r);
			half deltaDepth = m_depth - IN.proj.z;

			fixed4 water = (
			    tex2D(_WaterTex, IN.uv_WaterTex + float2(_SurfaceSpeed*_Time.x,0))
			    + tex2D(_WaterTex, float2(1-IN.uv_WaterTex.y,IN.uv_WaterTex.x) + float2(_SurfaceSpeed*_Time.x,0))
			)/2;
			fixed4 waterColor = tex2D(_GTex, float2(min(0.99, deltaDepth/_BasicColorDepth),0));// basic water color
			fixed4 noiseColor = tex2D(_NoiseTex, IN.uv_NoiseTex);
			fixed4 waveColor = tex2D(_WaveTex,
			    float2(
			        1-min(1, deltaDepth/_WaveNoiseDepth)
			        +_WaveShockRange*sin(_Time.x*-_WaveSpeed+noiseColor.r*_WaveNoiseRange)
			    ,1)
			+offset);
			fixed4 waveColor2 = tex2D(_WaveTex,
			    float2(
			        1-min(1, deltaDepth/_WaveNoiseDepth)
			        +_WaveShockRange*sin(_Time.x*-_WaveSpeed+_WaveInterval+noiseColor.r*_WaveNoiseRange)
			    ,1)
			+offset);
			
			waveColor.rgb *= (1-(sin(_Time.x*-_WaveSpeed+noiseColor.r*_WaveNoiseRange)+1)/2)*noiseColor.r;
			waveColor2.rgb *= (1-(sin(_Time.x*-_WaveSpeed+_WaveInterval+noiseColor.r*_WaveNoiseRange)+1)/2)*noiseColor.r;

			half Alpha_Need = max(max(waveColor2.r,max(waveColor2.g,waveColor2.b)),max(waveColor.r,max(waveColor.g,waveColor.b)));
			half water_A = 1 - min(1, deltaDepth/_WaveNoiseDepth);// wave & noise smooth range

			o.Normal = UnpackNormal(bumpColor).xyz;
			o.Specular = _Specular;
			o.Gloss = _Gloss;
			o.Albedo = waterColor.rgb //basic color
			           + (water.rgb * water.a) * water_A//noise color
			           + (waveColor.rgb + waveColor2.rgb) * water_A;//wave color
			o.Alpha = min(1, max(deltaDepth/_BasicColorDepth,Alpha_Need));//Basic Color smooth Rangemax
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
