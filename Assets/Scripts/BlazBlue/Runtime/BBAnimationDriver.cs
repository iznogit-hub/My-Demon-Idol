using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Seeks the current attack clip from the combat tick. The graph is rebuilt only when the clip changes.
    /// </summary>
    public class BBAnimationDriver : MonoBehaviour
    {
        PlayableGraph _graph;
        AnimationClipPlayable _playable;
        AnimationClip _clip;

        public void Seek(AnimationClip clip, float normalizedTime)
        {
            if (clip == null) return;
            Animator animator = GetComponentInChildren<Animator>();
            if (animator == null) return;
            if (!_graph.IsValid() || _clip != clip)
            {
                if (_graph.IsValid()) _graph.Destroy();
                _graph = PlayableGraph.Create("BBAnimation");
                _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                _playable = AnimationClipPlayable.Create(_graph, clip);
                var output = AnimationPlayableOutput.Create(_graph, "BB", animator);
                output.SetSourcePlayable(_playable);
                _graph.Play();
                _clip = clip;
            }
            _playable.SetTime(Mathf.Clamp01(normalizedTime) * clip.length);
            _graph.Evaluate(0f);
        }

        void OnDisable()
        {
            if (_graph.IsValid()) _graph.Destroy();
            _clip = null;
        }
    }
}
