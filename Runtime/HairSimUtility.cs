﻿using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using Unity.Collections;

namespace Unity.DemoTeam.Hair
{
	public static class HairSimUtility
	{
		//---------------
		// gpu resources

		public static bool CreateBuffer(ref ComputeBuffer buffer, string name, int count, int stride, ComputeBufferType type = ComputeBufferType.Default)
		{
			if (buffer != null && buffer.count == count && buffer.stride == stride && buffer.IsValid())
				return false;

			if (buffer != null)
				buffer.Release();

			buffer = new ComputeBuffer(count, stride, type);
			buffer.name = name;
			return true;
		}

		public static void ReleaseBuffer(ref ComputeBuffer buffer)
		{
			if (buffer != null)
			{
				buffer.Release();
				buffer = null;
			}
		}

		public static bool CreateVolume(ref RenderTexture volume, string name, int cells, RenderTextureFormat format)
		{
			if (volume != null && volume.width == cells && volume.format == format)
				return false;

			if (volume != null)
				volume.Release();

			RenderTextureDescriptor volumeDesc = new RenderTextureDescriptor()
			{
				dimension = TextureDimension.Tex3D,
				width = cells,
				height = cells,
				volumeDepth = cells,
				colorFormat = format,
				enableRandomWrite = true,
				msaaSamples = 1,
			};

			//Debug.Log("creating volume " + name);
			volume = new RenderTexture(volumeDesc);
			volume.wrapMode = TextureWrapMode.Clamp;
			volume.hideFlags = HideFlags.HideAndDontSave;
			volume.name = name;
			volume.Create();
			return true;
		}

		public static bool CreateVolume(ref RenderTexture volume, string name, int cellCount, GraphicsFormat cellFormat)
		{
			if (volume != null && volume.width == cellCount && volume.graphicsFormat == cellFormat)
				return false;

			if (volume != null)
				volume.Release();

			RenderTextureDescriptor volumeDesc = new RenderTextureDescriptor()
			{
				dimension = TextureDimension.Tex3D,
				width = cellCount,
				height = cellCount,
				volumeDepth = cellCount,
				graphicsFormat = cellFormat,
				enableRandomWrite = true,
				msaaSamples = 1,
			};

			//Debug.Log("creating volume " + name);
			volume = new RenderTexture(volumeDesc);
			volume.wrapMode = TextureWrapMode.Clamp;
			volume.hideFlags = HideFlags.HideAndDontSave;
			volume.name = name;
			volume.Create();
			return true;
		}

		public static void ReleaseVolume(ref RenderTexture volume)
		{
			if (volume != null)
			{
				volume.Release();
				volume = null;
			}
		}

		//----------
		// gpu data

		public static void PushComputeBufferData<T>(CommandBuffer cmd, in ComputeBuffer buffer, in NativeArray<T> bufferData) where T : struct
		{
#if UNITY_2021_1_OR_NEWER
			cmd.SetBufferData(buffer, bufferData);
#else
			cmd.SetComputeBufferData(buffer, bufferData);
#endif
		}

		public static void PushConstantBufferData<T>(CommandBuffer cmd, in ComputeBuffer cbuffer, in T cbufferData) where T : struct
		{
			var cbufferStaging = new NativeArray<T>(1, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
			{
				cbufferStaging[0] = cbufferData;
#if UNITY_2021_1_OR_NEWER
				cmd.SetBufferData(cbuffer, dataStaging);
#else
				cmd.SetComputeBufferData(cbuffer, cbufferStaging);
#endif
				cbufferStaging.Dispose();
			}
		}

		//------------------
		// gpu bind targets

		public interface IBindTarget
		{
			void BindConstantBuffer(int nameID, ComputeBuffer cbuffer);
			void BindComputeBuffer(int nameID, ComputeBuffer buffer);
			void BindComputeTexture(int nameID, RenderTexture texture);
			void BindKeyword(string name, bool value);
		}

		public struct BindTargetCompute : IBindTarget
		{
			public ComputeShader cs;
			public int kernel;

			public BindTargetCompute(ComputeShader cs, int kernel)
			{
				this.cs = cs;
				this.kernel = kernel;
			}

			public void BindConstantBuffer(int nameID, ComputeBuffer cbuffer) => cs.SetConstantBuffer(nameID, cbuffer, 0, cbuffer.stride);
			public void BindComputeBuffer(int nameID, ComputeBuffer buffer) => cs.SetBuffer(kernel, nameID, buffer);
			public void BindComputeTexture(int nameID, RenderTexture texture) => cs.SetTexture(kernel, nameID, texture);
			public void BindKeyword(string name, bool value) => CoreUtils.SetKeyword(cs, name, value);
		}

		public struct BindTargetComputeCmd : IBindTarget
		{
			public CommandBuffer cmd;
			public ComputeShader cs;
			public int kernel;

			public BindTargetComputeCmd(CommandBuffer cmd, ComputeShader cs, int kernel)
			{
				this.cmd = cmd;
				this.cs = cs;
				this.kernel = kernel;
			}

			public void BindConstantBuffer(int nameID, ComputeBuffer cbuffer) => cmd.SetComputeConstantBufferParam(cs, nameID, cbuffer, 0, cbuffer.stride);
			public void BindComputeBuffer(int nameID, ComputeBuffer buffer) => cmd.SetComputeBufferParam(cs, kernel, nameID, buffer);
			public void BindComputeTexture(int nameID, RenderTexture texture) => cmd.SetComputeTextureParam(cs, kernel, nameID, texture);
			public void BindKeyword(string name, bool value) => CoreUtils.SetKeyword(cmd, name, value);
		}

		public struct BindTargetGlobal : IBindTarget
		{
			public void BindConstantBuffer(int nameID, ComputeBuffer cbuffer) => Shader.SetGlobalConstantBuffer(nameID, cbuffer, 0, cbuffer.stride);
			public void BindComputeBuffer(int nameID, ComputeBuffer buffer) => Shader.SetGlobalBuffer(nameID, buffer);
			public void BindComputeTexture(int nameID, RenderTexture texture) => Shader.SetGlobalTexture(nameID, texture);
			public void BindKeyword(string name, bool value)
			{
				if (value)
					Shader.EnableKeyword(name);
				else
					Shader.DisableKeyword(name);
			}
		}

		public struct BindTargetGlobalCmd : IBindTarget
		{
			public CommandBuffer cmd;

			public BindTargetGlobalCmd(CommandBuffer cmd)
			{
				this.cmd = cmd;
			}

			public void BindConstantBuffer(int nameID, ComputeBuffer cbuffer) => cmd.SetGlobalConstantBuffer(cbuffer, nameID, 0, cbuffer.stride);
			public void BindComputeBuffer(int nameID, ComputeBuffer buffer) => cmd.SetGlobalBuffer(nameID, buffer);
			public void BindComputeTexture(int nameID, RenderTexture texture) => cmd.SetGlobalTexture(nameID, texture);
			public void BindKeyword(string name, bool value) => CoreUtils.SetKeyword(cmd, name, value);
		}

		public struct BindTargetMaterial : IBindTarget
		{
			public Material mat;

			public BindTargetMaterial(Material mat)
			{
				this.mat = mat;
			}

			public void BindConstantBuffer(int nameID, ComputeBuffer cbuffer) => mat.SetConstantBuffer(nameID, cbuffer, 0, cbuffer.stride);
			public void BindComputeBuffer(int nameID, ComputeBuffer buffer) => mat.SetBuffer(nameID, buffer);
			public void BindComputeTexture(int nameID, RenderTexture texture) => mat.SetTexture(nameID, texture);
			public void BindKeyword(string name, bool value) => CoreUtils.SetKeyword(mat, name, value);
		}

		//------------
		// reflection

		public static void InitializeStaticFields<T>(Type type, Func<string, T> construct)
		{
			foreach (var field in type.GetFields())
			{
				field.SetValue(null, construct(field.Name));
			}
		}

		public static void InitializeStructFields<TStruct, T>(ref TStruct data, Func<string, T> construct) where TStruct : struct
		{
			Type type = typeof(TStruct);
			var boxed = (object)data;
			foreach (var field in type.GetFields())
			{
				field.SetValue(boxed, construct(field.Name));
			}
			data = (TStruct)boxed;
		}

		public static void EnumerateFields(Type type, Action<int, System.Reflection.FieldInfo> visit)
		{
			var fields = type.GetFields();
			for (int i = 0; i != fields.Length; i++)
			{
				visit(i, fields[i]);
			}
		}
	}
}
