using System;
using System.Collections.Generic;
using System.Linq;

using Machine.Specifications.Sdk;
using Machine.Specifications.Utility;

namespace Machine.Specifications.Factories
{
  public class FieldInspectionArguments<T>
  {
    Type _target;
    Func<object> _instanceResolver;
    bool _ensureMaximumOfOne;
    AttributeFullName _attributeFullName;
    object _instance; 

    public ICollection<T> Items { get; private set; }

    public FieldInspectionArguments(Type target,
      Func<object> instanceResolver,
      ICollection<T> items,
      bool ensureMaximumOfOne,
      AttributeFullName attributeFullName)
    {
      _target = target;
      _instanceResolver = instanceResolver;
      _ensureMaximumOfOne = ensureMaximumOfOne;
      _attributeFullName = attributeFullName;
      _instance = instanceResolver();

      Items = items;
    }

    public bool IsTargetingAStatic
    {
      get { return _target.IsAbstract && _target.IsSealed; }
    }

    public void CollectFieldValue()
    {
      var fields = _target.GetInstanceFieldsOfUsage(_attributeFullName);
      if (_ensureMaximumOfOne && fields.Count() > 1)
      {
        throw new SpecificationUsageException(String.Format("You cannot have more than one {0} clause in {1}",
          fields.First().FieldType.Name,
          _target.FullName));
      }
      var field = fields.FirstOrDefault();
      if (field != null)
      {
        var val = (T) field.GetValue(_instance);
        Items.Add(val);
      }
    }

    public bool HasNoInstance
    {
      get { return _instance == null; }
    }

    public bool CannotProceed
    {
      get { return _target == typeof(object); }
    }

    public FieldInspectionArguments<T> DetailsForBaseType()
    {
      return new FieldInspectionArguments<T>(_target.BaseType, 
        _instanceResolver, 
        Items, 
        _ensureMaximumOfOne, 
        _attributeFullName);
    }

    public FieldInspectionArguments<T> DetailsForDeclaringType()
    {
      var declaringType = GetDeclaringTypeResolver()();
      return new FieldInspectionArguments<T>(declaringType,
        () => Activator.CreateInstance(declaringType),
        Items,
        _ensureMaximumOfOne,
        _attributeFullName);
    }

    public static FieldInspectionArguments<T> CreateFromInstance(object instance,
      bool ensureMaximumOfOne,
      AttributeFullName attributeFullName)
    {
      var delegates = new List<T>();
      var type = instance.GetType();

      return new FieldInspectionArguments<T>(type,
        () => instance,
        delegates,
        ensureMaximumOfOne,
        attributeFullName);
    }

    Func<Type> GetDeclaringTypeResolver()
    {
      Func<Type> declaringTypeResolution = () => _target.DeclaringType;
      Func<Type> resolveDeclaringTypeUsingAnInstanceToMaintainCorrectGenericParameters = () => GetDeclaringType();

      var typeResolver = _instance == null
        ? declaringTypeResolution
        : resolveDeclaringTypeUsingAnInstanceToMaintainCorrectGenericParameters;

      return typeResolver;
    }

    Type DeclaringType
    {
      get
      {
        return TargetType.DeclaringType;
      }
    }

    Type TargetType
    {
      get
      {
        return _instance.GetType();
      }
    }


    public bool IsNested
    {
      get
      {
        return TargetType.IsNested;
      }
    }


    bool DeclaringTypeIsObject
    {
      get
      {
        return DeclaringType == typeof(object);
      }
    }

    bool DeclaringTypeIsNotGeneric
    {
      get
      {
        return !DeclaringType.ContainsGenericParameters;
      }
    }

    int NumberOfGenericParametersOnDeclaringType
    {
      get
      {
        return DeclaringType.GetGenericTypeDefinition().GetGenericArguments().Count();
      }
    }

    Type MakeClosedVersionOfDeclaringType()
    {
      var parameters = GetGenericParametersForDeclaringType();

      return DeclaringType.MakeGenericType(parameters);
    }

    Type GetDeclaringType()
    {
      if (DeclaringTypeIsObject || DeclaringTypeIsNotGeneric)
        return DeclaringType;

      return MakeClosedVersionOfDeclaringType();
    }

    Type[] GetGenericParametersForDeclaringType()
    {
      var parameters = TargetType.GetGenericArguments()
        .Take(NumberOfGenericParametersOnDeclaringType);

      return parameters.ToArray();
    }

  }
}