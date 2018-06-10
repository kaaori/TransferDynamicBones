using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using VRCSDK2;

[CustomEditor(typeof(TransferDynamicBones))]
public class TransferDynamicBonesEditor : Editor
{
    private string labelText =
        "Drop your base model into the box below and click transfer";
    TransferDynamicBones obj;
    private GameObject _baseAvatar;
    private Animator _baseAnim;
    private List<Transform> _baseAllBones;

    private GameObject _targetAvatar;
    private Animator _targetAnim;
    private List<Transform> _targetAllBones;

    public void OnEnable()
    {
        obj = (TransferDynamicBones)target;

    }
    public override void OnInspectorGUI()
    {
        GUILayout.Label(labelText);
        EditorGUILayout.LabelField("Base Avatar:", EditorStyles.boldLabel);
        if (obj.GetComponent<VRC_AvatarDescriptor>() == null) 
        {
            obj.gameObject.AddComponent<VRC_AvatarDescriptor>();
        }
        _baseAvatar = (GameObject)EditorGUILayout.ObjectField(_baseAvatar, typeof(GameObject), true);
        // Check for VRC Avatar Descriptor
        if (_baseAvatar != null && _baseAvatar.GetComponent<VRC_AvatarDescriptor>() == null)
        {
            var result = EditorUtility.DisplayDialog("Error",
                "Selected object does not have a VRC Avatar Descriptor", "Add now", "Cancel");
            if (result)
            {
                _baseAvatar.AddComponent<VRC_AvatarDescriptor>();
            }
            else
            {
                _baseAvatar = null;
            }
        }
        if (!GUILayout.Button("Transfer bone settings"))
            return;
        DoEverything();
    }

    private void Cleanup()
    {
        // Remove this script from the avatar so that VRC is happy.
        DestroyImmediate(obj.gameObject.GetComponent<TransferDynamicBones>());
    }

    private void DoEverything()
    {
        try
        {
            _baseAnim = _baseAvatar.GetComponent<Animator>();
            _baseAllBones = _baseAnim.GetComponentsInChildren<Transform>().ToList();

            _targetAnim = obj.gameObject.GetComponent<Animator>();
            _targetAllBones = _targetAnim.GetComponentsInChildren<Transform>().ToList();

            // Filter to only matching bones on target model
            var filteredTargetBones = _targetAllBones.Where(item =>
                _baseAllBones.Any(category => category.name.Equals(item.name))).ToList();

            // Filter to only matching bones on base model
            var filteredBaseBones = _baseAllBones.Where(item =>
                _targetAllBones.Any(category => category.name.Equals(item.name))).ToList();

            foreach (var baseBone in filteredBaseBones)
            {

                if (baseBone.gameObject.GetComponent<DynamicBone>() != null)
                {
                    var targetBone = filteredTargetBones.First(x => x.name == baseBone.name).gameObject.transform;
                    var baseDynBone = baseBone.gameObject.GetComponent<DynamicBone>();
                    var targetDynBone = targetBone.GetComponent<DynamicBone>();
                    if (baseDynBone == null)
                    {
                        Debug.Log("Base Bone is null");
                        continue;
                    }
                    if (targetDynBone == null)
                    {
                        Debug.Log("Targ Bone is null, adding dynamic bone");
                        targetDynBone = filteredTargetBones.First(x => x.name == baseBone.name).gameObject
                            .AddComponent<DynamicBone>();
                    }
                    foreach (var f in baseDynBone.GetType().GetFields())
                    {
                        f.SetValue(targetDynBone, f.GetValue(baseDynBone));
                    }

                    targetDynBone.m_Root = targetBone;

                    Debug.Log("Got base bobe: " + baseDynBone.name + " from: " +baseDynBone.transform.root.name +
                              "\nGot targ bone: " + targetDynBone.name+ " from "+ targetDynBone.transform.root.name);
                }

                if (baseBone.gameObject.GetComponent<DynamicBoneCollider>() != null)
                {
                    var targetBone = filteredTargetBones.First(x => x.name == baseBone.name).gameObject.transform;
                    var baseCollider = baseBone.gameObject.GetComponent<DynamicBoneCollider>();
                    var targetCollider = targetBone.GetComponent<DynamicBoneCollider>();
                    if (baseCollider == null)
                    {
                        Debug.Log("Base collider is null");
                        continue;
                    }
                    if (targetCollider == null)
                    {
                        Debug.Log("Targ Bone is null, adding dynamic bone");
                        targetCollider = filteredTargetBones.First(x => x.name == baseBone.name).gameObject
                            .AddComponent<DynamicBoneCollider>();
                    }
                    foreach (var f in baseCollider.GetType().GetFields())
                    {
                        f.SetValue(targetCollider, f.GetValue(baseCollider));
                    }
                }
            }
            Cleanup();
        }
        catch (System.Exception ex)
        {
            Debug.Log("ERROR: "+ex);
        }
    }
}
