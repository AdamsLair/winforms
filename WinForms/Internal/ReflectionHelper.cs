using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AdamsLair.WinForms
{
	internal static class ReflectionHelper
	{
		public static object CreateInstanceOf(this Type instanceType, bool noConstructor = false)
		{
			try
			{
				if (instanceType == typeof(string))
					return "";
				else if (typeof(Array).IsAssignableFrom(instanceType) && instanceType.GetArrayRank() == 1)
					return Array.CreateInstance(instanceType.GetElementType(), 0);
				else if (noConstructor)
					return System.Runtime.Serialization.FormatterServices.GetUninitializedObject(instanceType);
				else
					return Activator.CreateInstance(instanceType, true);
			}
			catch (Exception)
			{
				return null;
			}
		}
		public static bool IsDerivedFrom(this Type type, Type baseType)
		{
			do
			{
				if (type.BaseType == baseType)
					return true;

				type = type.BaseType;
			} while (type != null);

			return false;
		}
		public static Type GetCommonBaseClass(this IEnumerable<Type> types)
		{
			Type commonBase = null;
			foreach (Type type in types)
			{
				if (commonBase == null)
				{
					commonBase = type;
					continue;
				}
				while (commonBase != null && !commonBase.IsAssignableFrom(type))
				{
					commonBase = commonBase.BaseType ?? typeof(object);
					if (commonBase == typeof(object)) return commonBase;
				}
			}
			return commonBase;
		}
		public static string GetTypeCSCodeName(this Type T, bool shortName = false)
		{
			StringBuilder typeStr = new StringBuilder();

			if (T.IsGenericParameter)
			{
				return T.Name;
			}
			if (T.IsArray)
			{
				typeStr.Append(GetTypeCSCodeName(T.GetElementType(), shortName));
				typeStr.Append('[');
				typeStr.Append(',', T.GetArrayRank() - 1);
				typeStr.Append(']');
			}
			else
			{
				Type[] genArgs = T.IsGenericType ? T.GetGenericArguments() : null;

				if (T.IsNested)
				{
					Type declType = T.DeclaringType;
					if (declType.IsGenericTypeDefinition)
					{
						Array.Resize(ref genArgs, declType.GetGenericArguments().Length);
						declType = declType.MakeGenericType(genArgs);
						genArgs = T.GetGenericArguments().Skip(genArgs.Length).ToArray();
					}
					string parentName = GetTypeCSCodeName(declType, shortName);

					string[] nestedNameToken = shortName ? T.Name.Split('+') : T.FullName.Split('+');
					string nestedName = nestedNameToken[nestedNameToken.Length - 1];
						
					int genTypeSepIndex = nestedName.IndexOf("[[");
					if (genTypeSepIndex != -1) nestedName = nestedName.Substring(0, genTypeSepIndex);
					genTypeSepIndex = nestedName.IndexOf('`');
					if (genTypeSepIndex != -1) nestedName = nestedName.Substring(0, genTypeSepIndex);

					typeStr.Append(parentName);
					typeStr.Append('.');
					typeStr.Append(nestedName);
				}
				else
				{
					if (shortName)
						typeStr.Append(T.Name.Split(new char[] {'`'}, StringSplitOptions.RemoveEmptyEntries)[0].Replace('+', '.'));
					else
						typeStr.Append(T.FullName.Split(new char[] {'`'}, StringSplitOptions.RemoveEmptyEntries)[0].Replace('+', '.'));
				}

				if (genArgs != null && genArgs.Length > 0)
				{
					if (T.IsGenericTypeDefinition)
					{
						typeStr.Append('<');
						typeStr.Append(',', genArgs.Length - 1);
						typeStr.Append('>');
					}
					else if (T.IsGenericType)
					{
						typeStr.Append('<');
						for (int i = 0; i < genArgs.Length; i++)
						{
							typeStr.Append(GetTypeCSCodeName(genArgs[i], shortName));
							if (i < genArgs.Length - 1)
								typeStr.Append(',');
						}
						typeStr.Append('>');
					}
				}
			}

			return typeStr.Replace('+', '.').ToString();
		}

		public static Type[] FindConcreteTypes(Type abstractType)
		{
			return AppDomain.CurrentDomain.GetAssemblies().
				Where(a => !a.IsDynamic).
				SelectMany(a => a.GetExportedTypes()).
				Where(t => !t.IsAbstract && !t.IsInterface && abstractType.IsAssignableFrom(t)).
				ToArray();
		}
	}
}
