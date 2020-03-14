// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using UnityEditor;
using UnityEngine;

namespace Microsoft.MixedReality.WebRTC.Unity.Editor
{
    /// <summary>
    /// Inspector editor for <see cref="WebcamSource"/>.
    /// </summary>
    [CustomEditor(typeof(WebcamSource))]
    [CanEditMultipleObjects]
    public class WebcamSourceEditor : UnityEditor.Editor
    {
        SerializedProperty _trackName;
        SerializedProperty _autoStartOnEnabled;
        SerializedProperty _preferredVideoCodec;
        SerializedProperty _enableMixedRealityCapture;
        SerializedProperty _enableMrcRecordingIndicator;
        SerializedProperty _formatMode;
        SerializedProperty _videoProfileId;
        SerializedProperty _videoProfileKind;
        SerializedProperty _constraints;
        SerializedProperty _width;
        SerializedProperty _height;
        SerializedProperty _framerate;
        SerializedProperty _videoStreamStarted;
        SerializedProperty _videoStreamStopped;

        GUIContent _anyContent;
        float _anyWidth;
        float _unitWidth;

        int _prevWidth = 640;
        int _prevHeight = 480;
        double _prevFramerate = 30.0;
        VideoProfileKind _prevVideoProfileKind = VideoProfileKind.VideoConferencing;
        string _prevVideoProfileId = "<profile id>";

        /// <summary>
        /// Helper enumeration for commonly used video codecs.
        /// The enum names must match exactly the standard SDP naming.
        /// See https://en.wikipedia.org/wiki/RTP_audio_video_profile for reference.
        /// </summary>
        enum SdpVideoCodecs
        {
            /// <summary>
            /// Do not force any codec, let WebRTC decide.
            /// </summary>
            None,

            /// <summary>
            /// Try to use H.264 if available.
            /// </summary>
            H264,

            /// <summary>
            /// Try to use VP8 if available.
            /// </summary>
            VP8,

            /// <summary>
            /// Try to use VP9 if available.
            /// </summary>
            VP9,

            /// <summary>
            /// Try to use the given codec if available.
            /// </summary>
            Custom
        }

        void OnEnable()
        {
            _trackName = serializedObject.FindProperty("TrackName");
            _autoStartOnEnabled = serializedObject.FindProperty("AutoStartOnEnabled");
            _preferredVideoCodec = serializedObject.FindProperty("PreferredVideoCodec");
            _enableMixedRealityCapture = serializedObject.FindProperty("EnableMixedRealityCapture");
            _enableMrcRecordingIndicator = serializedObject.FindProperty("EnableMRCRecordingIndicator");
            _formatMode = serializedObject.FindProperty("FormatMode");
            _videoProfileId = serializedObject.FindProperty("VideoProfileId");
            _videoProfileKind = serializedObject.FindProperty("VideoProfileKind");
            _constraints = serializedObject.FindProperty("Constraints");
            _width = _constraints.FindPropertyRelative("width");
            _height = _constraints.FindPropertyRelative("height");
            _framerate = _constraints.FindPropertyRelative("framerate");
            _videoStreamStarted = serializedObject.FindProperty("VideoStreamStarted");
            _videoStreamStopped = serializedObject.FindProperty("VideoStreamStopped");

            _anyContent = new GUIContent("(any)");
            _anyWidth = -1f; // initialized later
            _unitWidth = -1f; // initialized later
        }

        /// <summary>
        /// Override implementation of <a href="https://docs.unity3d.com/ScriptReference/Editor.OnInspectorGUI.html">Editor.OnInspectorGUI</a>
        /// to draw the inspector GUI for the currently selected <see cref="WebcamSource"/>.
        /// </summary>
        public override void OnInspectorGUI()
        {
            // CalcSize() can only be called inside a GUI method
            if (_anyWidth < 0)
                _anyWidth = GUI.skin.label.CalcSize(_anyContent).x;
            if (_unitWidth < 0)
                _unitWidth = GUI.skin.label.CalcSize(new GUIContent("fps")).x;

            serializedObject.Update();

            GUILayout.Space(10);

            EditorGUILayout.LabelField("Video capture", EditorStyles.boldLabel);
            ToggleLeft(_autoStartOnEnabled,
                    new GUIContent("Start capture when enabled", "Automatically start video capture when this component is enabled."));
            EditorGUILayout.PropertyField(_formatMode, new GUIContent("Capture format",
                "Decide how to obtain the constraints used to select the best capture format."));
            if ((LocalVideoSourceFormatMode)_formatMode.intValue == LocalVideoSourceFormatMode.Manual)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.LabelField("General constraints (all platforms)");
                    using (new EditorGUI.IndentLevelScope())
                    {
                        OptionalIntField(_width, ref _prevWidth,
                            new GUIContent("Width", "Only consider capture formats with the specified width."),
                            new GUIContent("px", "Pixels"));
                        OptionalIntField(_height, ref _prevHeight,
                            new GUIContent("Height", "Only consider capture formats with the specified height."),
                            new GUIContent("px", "Pixels"));
                        OptionalDoubleField(_framerate, ref _prevFramerate,
                            new GUIContent("Framerate", "Only consider capture formats with the specified framerate."),
                            new GUIContent("fps", "Frames per second"));
                    }

                    EditorGUILayout.LabelField("UWP constraints");
                    using (new EditorGUI.IndentLevelScope())
                    {
                        OptionalEnumField(_videoProfileKind, VideoProfileKind.Unspecified, ref _prevVideoProfileKind,
                            new GUIContent("Video profile kind", "Only consider capture formats associated with the specified video profile kind."));
                        OptionalTextField(_videoProfileId, ref _prevVideoProfileId,
                            new GUIContent("Video profile ID", "Only consider capture formats associated with the specified video profile."));
                        if ((_videoProfileKind.intValue != (int)VideoProfileKind.Unspecified) && (_videoProfileId.stringValue.Length > 0))
                        {
                            EditorGUILayout.HelpBox("Video profile ID is already unique. Specifying also a video kind over-constrains the selection algorithm and can decrease the chances of finding a matching video profile. It is recommended to select either a video profile kind, or a video profile ID.", MessageType.Warning);
                        }
                    }
                }
            }
            _enableMixedRealityCapture.boolValue = EditorGUILayout.ToggleLeft("Enable Mixed Reality Capture (MRC)", _enableMixedRealityCapture.boolValue);
            if (_enableMixedRealityCapture.boolValue)
            {
                using (var scope = new EditorGUI.IndentLevelScope())
                {
                    _enableMrcRecordingIndicator.boolValue = EditorGUILayout.ToggleLeft("Show recording indicator in device", _enableMrcRecordingIndicator.boolValue);
                    if (!PlayerSettings.virtualRealitySupported)
                    {
                        EditorGUILayout.HelpBox("Mixed Reality Capture can only work in exclusive-mode apps. XR support must be enabled in Project Settings > Player > XR Settings > Virtual Reality Supported, and the project then saved to disk.", MessageType.Error);
                        if (GUILayout.Button("Enable XR support"))
                        {
                            PlayerSettings.virtualRealitySupported = true;
                        }
                    }
                }
            }
            EditorGUILayout.PropertyField(_videoStreamStarted);
            EditorGUILayout.PropertyField(_videoStreamStopped);

            GUILayout.Space(10);

            EditorGUILayout.LabelField("WebRTC track", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_trackName);

            var rect = EditorGUILayout.GetControlRect();
            using (new EditorGUI.PropertyScope(rect, new GUIContent(), _preferredVideoCodec))
            {
                try
                {
                    // Convert the selected codec name to an enum value.
                    // This may throw an exception if this is a custom name, which will be handled below.
                    SdpVideoCodecs codecValue;
                    string customCodecValue = string.Empty;
                    if (_preferredVideoCodec.stringValue.Length == 0)
                    {
                        codecValue = SdpVideoCodecs.None;
                    }
                    else
                    {
                        try
                        {
                            codecValue = (SdpVideoCodecs)Enum.Parse(typeof(SdpVideoCodecs), _preferredVideoCodec.stringValue);
                        }
                        catch
                        {
                            codecValue = SdpVideoCodecs.Custom;
                            customCodecValue = _preferredVideoCodec.stringValue;
                            // Hide internal marker
                            if (customCodecValue == "__CUSTOM")
                            {
                                customCodecValue = string.Empty;
                            }
                        }
                    }

                    // Display the edit field for the enum
                    var newCodecValue = (SdpVideoCodecs)EditorGUI.EnumPopup(rect, _preferredVideoCodec.displayName, codecValue);
                    if (newCodecValue == SdpVideoCodecs.H264)
                    {
                        EditorGUILayout.HelpBox("H.264 is only supported on UWP platforms.", MessageType.Warning);
                    }

                    // Update the value if changed or custom
                    if ((newCodecValue != codecValue) || (newCodecValue == SdpVideoCodecs.Custom))
                    {
                        if (newCodecValue == SdpVideoCodecs.None)
                        {
                            _preferredVideoCodec.stringValue = string.Empty;
                        }
                        else if (newCodecValue == SdpVideoCodecs.Custom)
                        {
                            ++EditorGUI.indentLevel;
                            string newValue = EditorGUILayout.TextField("SDP codec name", customCodecValue);
                            if (newValue == string.Empty)
                            {
                                EditorGUILayout.HelpBox("The SDP codec name must be non-empty. See https://en.wikipedia.org/wiki/RTP_audio_video_profile for valid names.", MessageType.Error);

                                // Force a non-empty value now, otherwise the field will reset to None
                                newValue = "__CUSTOM";
                            }
                            _preferredVideoCodec.stringValue = newValue;
                            --EditorGUI.indentLevel;
                        }
                        else
                        {
                            _preferredVideoCodec.stringValue = Enum.GetName(typeof(SdpVideoCodecs), newCodecValue);
                        }
                    }
                }
                catch (Exception)
                {
                    EditorGUILayout.PropertyField(_preferredVideoCodec);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// ToggleLeft control associated with a given SerializedProperty, to enable automatic GUI
        /// handlings like Prefab revert menu.
        /// </summary>
        /// <param name="property">The boolean property associated with the control.</param>
        /// <param name="label">The label to display next to the toggle control.</param>
        private void ToggleLeft(SerializedProperty property, GUIContent label)
        {
            var rect = EditorGUILayout.GetControlRect();
            using (new EditorGUI.PropertyScope(rect, label, property))
            {
                property.boolValue = EditorGUI.ToggleLeft(rect, label, property.boolValue);
            }
        }

        /// <summary>
        /// IntField with optional toggle associated with a given SerializedProperty, to enable
        /// automatic GUI handlings like Prefab revert menu.
        /// </summary>
        /// <param name="intProperty">The integer property associated with the control.</param>
        /// <param name="defaultValue">Default value if the property value is negative or zero. Assigned the new value on return if valid.</param>
        /// <param name="label">The label to display next to the toggle control.</param>
        /// <param name="unitLabel">The label indicating the unit of the value.</param>
        private void OptionalIntField(SerializedProperty intProperty, ref int defaultValue, GUIContent label, GUIContent unitLabel)
        {
            if (defaultValue <= 0)
            {
                throw new ArgumentOutOfRangeException("Default value cannot be invalid.");
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                var rect = EditorGUILayout.GetControlRect();
                using (new EditorGUI.PropertyScope(rect, label, intProperty))
                {
                    bool hasConstraint = (intProperty.intValue > 0);
                    hasConstraint = EditorGUI.ToggleLeft(rect, label, hasConstraint);
                    int newValue = intProperty.intValue;
                    if (hasConstraint)
                    {
                        // Force a valid value, otherwise the edit field won't show up
                        if (newValue <= 0)
                        {
                            newValue = defaultValue;
                        }

                        // Make this delayed to allow override for negative numbers before it takes effect.
                        // This allows the user to type "-5" and only on Return/Enter or focus loss see the
                        // value change to 0.
                        newValue = EditorGUILayout.DelayedIntField(newValue);
                        if (newValue < 0)
                        {
                            newValue = 0;
                        }
                    }
                    else
                    {
                        // Force value for consistency with "constraint", otherwise this breaks Prefab revert
                        newValue = 0;
                    }
                    intProperty.intValue = newValue;
                    if (newValue > 0)
                    {
                        GUILayout.Label(unitLabel, GUILayout.Width(_unitWidth));

                        // Save valid value as new default. This allows toggling the constraint ON and OFF
                        // without losing the value previously input. This works only while the inspector is
                        // alive, that is while the object is select, but is better than nothing.
                        defaultValue = newValue;
                    }
                    else
                    {
                        GUILayout.Label(_anyContent, GUILayout.Width(_anyWidth));
                    }
                }
            }
        }

        /// <summary>
        /// DoubleField with optional toggle associated with a given SerializedProperty, to enable
        /// automatic GUI handlings like Prefab revert menu.
        /// </summary>
        /// <param name="doubleProperty">The double property associated with the control.</param>
        /// <param name="defaultValue">Default value if the property value is negative or zero. Assigned the new value on return if valid.</param>
        /// <param name="label">The label to display next to the toggle control.</param>
        /// <param name="unitLabel">The label indicating the unit of the value.</param>
        private void OptionalDoubleField(SerializedProperty doubleProperty, ref double defaultValue, GUIContent label, GUIContent unitLabel)
        {
            if (defaultValue <= 0.0)
            {
                throw new ArgumentOutOfRangeException("Default value cannot be invalid.");
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                var rect = EditorGUILayout.GetControlRect();
                using (new EditorGUI.PropertyScope(rect, label, doubleProperty))
                {
                    bool hasConstraint = (doubleProperty.doubleValue > 0.0);
                    hasConstraint = EditorGUI.ToggleLeft(rect, label, hasConstraint);
                    double newValue = doubleProperty.doubleValue;
                    if (hasConstraint)
                    {
                        // Force a valid value, otherwise the edit field won't show up
                        if (newValue <= 0.0)
                        {
                            newValue = defaultValue;
                        }

                        // Make this delayed to allow override for negative numbers before it takes effect.
                        // This allows the user to type "-5" and only on Return/Enter or focus loss see the
                        // value change to 0.
                        newValue = EditorGUILayout.DelayedDoubleField(newValue);
                        if (newValue < 0.0)
                        {
                            newValue = 0.0;
                        }
                    }
                    else
                    {
                        // Force value for consistency with "constraint", otherwise this breaks Prefab revert
                        newValue = 0.0;
                    }
                    doubleProperty.doubleValue = newValue;
                    if (newValue > 0.0)
                    {
                        GUILayout.Label(unitLabel, GUILayout.Width(_unitWidth));

                        // Save valid value as new default. This allows toggling the constraint ON and OFF
                        // without losing the value previously input. This works only while the inspector is
                        // alive, that is while the object is select, but is better than nothing.
                        defaultValue = newValue;
                    }
                    else
                    {
                        GUILayout.Label(_anyContent, GUILayout.Width(_anyWidth));
                    }
                }
            }
        }

        /// <summary>
        /// Helper to convert an enum to its integer value.
        /// </summary>
        /// <typeparam name="TValue">The enum type.</typeparam>
        /// <param name="value">The enum value.</param>
        /// <returns>The integer value associated with <see cref="value"/>.</returns>
        public static int EnumToInt<TValue>(TValue value) where TValue : Enum => (int)(object)value;

        /// <summary>
        /// Helper to convert an integer to its enum value.
        /// </summary>
        /// <typeparam name="TValue">The enum type.</typeparam>
        /// <param name="value">The integer value.</param>
        /// <returns>The enum value whose integer value is <see cref="value"/>.</returns>
        public static TValue IntToEnum<TValue>(int value) where TValue : Enum => (TValue)(object)value;

        /// <summary>
        /// EnumPopup with optional toggle associated with a given SerializedProperty, to enable
        /// automatic GUI handlings like Prefab revert menu.
        /// </summary>
        /// <param name="enumProperty">The enum property associated with the control.</param>
        /// <param name="nilValue">Value considered to be "invalid", which deselects the toggle control.</param>
        /// <param name="defaultValue">Default value if the property value is not <see cref="nilValue"/>. Assigned the new value on return if not <see cref="nilValue"/>.</param>
        /// <param name="label">The label to display next to the toggle control.</param>
        private void OptionalEnumField<T>(SerializedProperty enumProperty, T nilValue, ref T defaultValue, GUIContent label) where T : Enum
        {
            if (nilValue.CompareTo(defaultValue) == 0)
            {
                throw new ArgumentOutOfRangeException("Default value cannot be invalid.");
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                var rect = EditorGUILayout.GetControlRect();
                using (new EditorGUI.PropertyScope(rect, label, enumProperty))
                {
                    bool hasConstraint = (enumProperty.intValue != EnumToInt<T>(nilValue));
                    hasConstraint = EditorGUI.ToggleLeft(rect, label, hasConstraint);
                    T newValue = IntToEnum<T>(enumProperty.intValue);
                    if (hasConstraint)
                    {
                        // Force a valid value, otherwise the popup control won't show up
                        if (newValue.CompareTo(nilValue) == 0)
                        {
                            newValue = defaultValue;
                        }

                        newValue = (T)EditorGUILayout.EnumPopup(newValue);
                    }
                    else
                    {
                        // Force value for consistency with "constraint", otherwise this breaks Prefab revert
                        newValue = nilValue;
                    }
                    enumProperty.intValue = EnumToInt<T>(newValue);
                    if (newValue.CompareTo(nilValue) != 0)
                    {
                        // Save valid value as new default. This allows toggling the constraint ON and OFF
                        // without losing the value previously input. This works only while the inspector is
                        // alive, that is while the object is select, but is better than nothing.
                        defaultValue = newValue;
                    }
                    else
                    {
                        GUILayout.Label(_anyContent, GUILayout.Width(_anyWidth));
                    }
                }
            }
        }

        /// <summary>
        /// TextField with optional toggle associated with a given SerializedProperty, to enable
        /// automatic GUI handlings like Prefab revert menu.
        /// </summary>
        /// <param name="stringProperty">The string property associated with the control.</param>
        /// <param name="defaultValue">Default value if the property value null or whitespace. Assigned the new value on return if valid.</param>
        /// <param name="label">The label to display next to the toggle control.</param>
        private void OptionalTextField(SerializedProperty stringProperty, ref string defaultValue, GUIContent label)
        {
            // Note: there is a small bug in this, if the user enters a valid value on a prefab instance,
            // validates (Return/Enter/unfocus), then select the DoubleField, then click RMB > Revert, then
            // the property value is reset but the DoubleField value stays the same instead of being cleared.
            // If the user does not reselect the DoubleField however, which is the most common case, things
            // work as expected. So this rarely happens.

            if (string.IsNullOrWhiteSpace(defaultValue))
            {
                throw new ArgumentOutOfRangeException("Default value cannot be invalid.");
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                var rect = EditorGUILayout.GetControlRect();
                using (new EditorGUI.PropertyScope(rect, label, stringProperty))
                {
                    bool hasConstraint = !string.IsNullOrWhiteSpace(stringProperty.stringValue);
                    hasConstraint = EditorGUI.ToggleLeft(rect, label, hasConstraint);
                    string newValue = stringProperty.stringValue;
                    if (hasConstraint)
                    {
                        // Force a valid value, otherwise the edit field won't show up
                        if (string.IsNullOrWhiteSpace(newValue))
                        {
                            newValue = defaultValue;
                        }

                        // Make this delayed to allow override for negative numbers before it takes effect.
                        // This allows the user to type "-5" and only on Return/Enter or focus loss see the
                        // value change to 0.
                        newValue = EditorGUILayout.DelayedTextField(newValue);
                        if (string.IsNullOrWhiteSpace(newValue))
                        {
                            newValue = string.Empty;
                        }
                    }
                    else
                    {
                        // Force value for consistency with "constraint", otherwise this breaks Prefab revert
                        newValue = string.Empty;
                    }
                    stringProperty.stringValue = newValue;
                    if (!string.IsNullOrWhiteSpace(newValue))
                    {
                        // Save valid value as new default. This allows toggling the constraint ON and OFF
                        // without losing the value previously input. This works only while the inspector is
                        // alive, that is while the object is select, but is better than nothing.
                        defaultValue = newValue;
                    }
                    else
                    {
                        GUILayout.Label(_anyContent, GUILayout.Width(_anyWidth));
                    }
                }
            }
        }
    }
}
