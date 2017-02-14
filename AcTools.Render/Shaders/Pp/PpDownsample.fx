// textures
Texture2D gInputMap;

SamplerState samPoint {
	Filter = MIN_MAG_MIP_POINT;
	AddressU = CLAMP;
	AddressV = CLAMP;
};

SamplerState samLinear {
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = CLAMP;
	AddressV = CLAMP;
};
	
// input resources
cbuffer cbPerObject : register(b0) {
	float4 gScreenSize;
	float2 gMultipler; // less than zero
}

// fn structs
struct VS_IN {
	float3 PosL    : POSITION;
	float2 Tex     : TEXCOORD;
};

struct PS_IN {
	float4 PosH    : SV_POSITION;
	float2 Tex     : TEXCOORD;
};

// one vertex shader for everything
PS_IN vs_main(VS_IN vin) {
	PS_IN vout;
	vout.PosH = float4(vin.PosL, 1.0);
	vout.Tex = vin.Tex;
	return vout;
}

// just copy to the output buffer
float4 ps_Copy(PS_IN pin) : SV_Target{
	return gInputMap.Sample(samPoint, pin.Tex);
}

	technique10 Copy {
	pass P0 {
		SetVertexShader(CompileShader(vs_4_0, vs_main()));
		SetGeometryShader(NULL);
		SetPixelShader(CompileShader(ps_4_0, ps_Copy()));
	}
}

// just copy to the output buffer
float4 ps_Average(PS_IN pin) : SV_Target {
	float4 result = 0;
	float v = 0;

	float x, y;
	for (x = -1; x <= 1; x += 0.25) {
		for (y = -1; y <= 1; y += 0.25) {
			float2 uv = pin.Tex + float2(x, y) * gScreenSize.zw * gMultipler * 1.2;
			float w = sqrt((1.1 - abs(x)) * 2 + (1.1 - abs(y)) * 2);
			result += gInputMap.Sample(samPoint, uv) * w;
			v += w;
		}
	}

	return result / v;
}

technique10 Average {
	pass P0 {
		SetVertexShader(CompileShader(vs_4_0, vs_main()));
		SetGeometryShader(NULL);
		SetPixelShader(CompileShader(ps_4_0, ps_Average()));
	}
}

// anisotropic thing
SamplerState samAnisotropic {
	Filter = ANISOTROPIC;
	MaxAnisotropy = 16;

	AddressU = WRAP;
	AddressV = WRAP;
};

float4 ps_Anisotropic(PS_IN pin) : SV_Target {
	return gInputMap.Sample(samAnisotropic, pin.Tex);
}

technique10 Anisotropic {
	pass P0 {
		SetVertexShader(CompileShader(vs_4_0, vs_main()));
		SetGeometryShader(NULL);
		SetPixelShader(CompileShader(ps_4_0, ps_Anisotropic()));
	}
}

// found online
float4 SampleBicubic(Texture2D tex, sampler texSampler, float2 uv){
	//--------------------------------------------------------------------------------------
	// Calculate the center of the texel to avoid any filtering

	float2 textureDimensions = gScreenSize.xy;
	float2 invTextureDimensions = gScreenSize.zw;

	uv *= textureDimensions;

	float2 texelCenter = floor(uv - 0.5f) + 0.5f;
	float2 fracOffset = uv - texelCenter;
	float2 fracOffset_x2 = fracOffset * fracOffset;
	float2 fracOffset_x3 = fracOffset * fracOffset_x2;

	//--------------------------------------------------------------------------------------
	// Calculate the filter weights (B-Spline Weighting Function)

	float2 weight0 = fracOffset_x2 - 0.5f * (fracOffset_x3 + fracOffset);
	float2 weight1 = 1.5f * fracOffset_x3 - 2.5f * fracOffset_x2 + 1.f;
	float2 weight3 = 0.5f * (fracOffset_x3 - fracOffset_x2);
	float2 weight2 = 1.f - weight0 - weight1 - weight3;

	//--------------------------------------------------------------------------------------
	// Calculate the texture coordinates

	float2 scalingFactor0 = weight0 + weight1;
	float2 scalingFactor1 = weight2 + weight3;

	float2 f0 = weight1 / (weight0 + weight1);
	float2 f1 = weight3 / (weight2 + weight3);

	float2 texCoord0 = texelCenter - 1.f + f0;
	float2 texCoord1 = texelCenter + 1.f + f1;

	texCoord0 *= invTextureDimensions;
	texCoord1 *= invTextureDimensions;

	//--------------------------------------------------------------------------------------
	// Sample the texture

	return tex.Sample(texSampler, float2(texCoord0.x, texCoord0.y)) * scalingFactor0.x * scalingFactor0.y +
		tex.Sample(texSampler, float2(texCoord1.x, texCoord0.y)) * scalingFactor1.x * scalingFactor0.y +
		tex.Sample(texSampler, float2(texCoord0.x, texCoord1.y)) * scalingFactor0.x * scalingFactor1.y +
		tex.Sample(texSampler, float2(texCoord1.x, texCoord1.y)) * scalingFactor1.x * scalingFactor1.y;
}

float4 ps_Bicubic(PS_IN pin) : SV_Target {
	return SampleBicubic(gInputMap, samPoint, pin.Tex);
}

technique10 Bicubic {
	pass P0 {
		SetVertexShader(CompileShader(vs_4_0, vs_main()));
		SetGeometryShader(NULL);
		SetPixelShader(CompileShader(ps_4_0, ps_Bicubic()));
	}
}