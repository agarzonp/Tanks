using UnityEngine;
using NPBehave;
using System.Collections.Generic;

namespace Complete
{
    /*
    Example behaviour trees for the Tank AI.  This is partial definition:
    the core AI code is defined in TankAI.cs.

    Use this file to specifiy your new behaviour tree.
     */
    public partial class TankAI : MonoBehaviour
    {
        private Root CreateBehaviourTree() {

            switch (m_Behaviour) {

                case 1:
                    return SpinBehaviour(-0.05f, 1f);
                case 2:
                    return TrackBehaviour();

                case 3:
                    return TankBehaviour();

                default:
                    return new Root (new Action(()=> Turn(0.1f)));
            }
        }

        /* Nodes */

      private Node DummyBehaviour()
      {
        return new Selector( 
            // select a different behaviour according to the health
            new BlackboardCondition("lowHealth", Operator.IS_EQUAL, true,
                                    Stops.IMMEDIATE_RESTART,
                                    // just track
                                    Track()),
            // March towards target
            MarchTowardsTarget());
      }

      private Node MarchTowardsTarget()
      {
        Debug.Log("===================== MarchTowardsTarget ================");
        return new Selector(
            ResolveBlockedPath(), 
            ShootTarget(), 
            MoveTowardsTarget());
      }

      private Node ResolveBlockedPath()
      {
        Debug.Log("ResolveBlockedPath: " + (blackboard != null ? blackboard["isBlocked"].Equals(true) : false));

        // check if we are blocked by using the blackboard
        return new BlackboardCondition("isBlocked", Operator.IS_EQUAL, true,
                                        Stops.IMMEDIATE_RESTART,
                                        // unblock sequence
                                        new Sequence(
                                            new Action(() => SetResolvingBlockedPath(true)),
                                            StopTurning(),
                                            StopMoving(),
                                            new Action(() => Move(-1.0f)),
                                            new Wait(0.2f),
                                            StopMoving(),
                                            new Action(() => Turn(1.0f)),
                                            new Wait(0.2f),
                                            StopTurning(),
                                            new Action(() => SetResolvingBlockedPath(false))));
      }

      private Node ShootTarget()
      {
        Debug.Log("ShootTarget: " + (blackboard != null ? blackboard["targetDistance"].ToString() : ""));

        // check if we are within distance by using the blackboard
        return new BlackboardCondition("targetDistance", Operator.IS_SMALLER_OR_EQUAL, 15.0f,
                                       Stops.IMMEDIATE_RESTART,
                                       new Sequence(
                                        // shoot sequence
                                        StopTurning(),
                                        StopMoving(),
                                        Track()));
      }

      private Node MoveTowardsTarget()
      {
        Debug.Log("MoveTowardsTarget");

        return new Selector(
          // check if the target is in front by using the blackboard
          new BlackboardCondition("targetInFront", Operator.IS_EQUAL, false,
                                  Stops.IMMEDIATE_RESTART,
                                  // turn towards target
                                  new Sequence(
                                        StopMoving(),
                                        TurnTowardsTarget())),
        // continue moving straight
        new Action(() => Move(0.5f)));  
      }

      private Node TurnTowardsTarget()
      {
        Debug.Log("TurnTowardsTarget");

        return new Selector(                
          new BlackboardCondition("targetOffCentre", Operator.IS_SMALLER_OR_EQUAL, 0.1f,   
                                  Stops.IMMEDIATE_RESTART,
                                    // Stop turning
                                    StopTurning()),
          new BlackboardCondition("targetOnRight", Operator.IS_EQUAL, true, 
                                  Stops.IMMEDIATE_RESTART,
                                  // Turn right toward target
                                  new Action(() => Turn(0.3f))),
          // Turn left toward target
          new Action(() => Turn(-0.3f)));
      }

      private Node Track()
      {
        Debug.Log("Track");
        return new Selector(
                          new BlackboardCondition("targetOffCentre", Operator.IS_SMALLER_OR_EQUAL, 0.1f,
                                                  Stops.IMMEDIATE_RESTART,
                              // Stop turning and fire
                              new Sequence(StopTurning(),
                                          RandomFire())),
                          new BlackboardCondition("targetOnRight", Operator.IS_EQUAL, true, Stops.IMMEDIATE_RESTART,
                              // Turn right toward target
                              new Action(() => Turn(0.8f))),
                              // Turn left toward target
                              new Action(() => Turn(-0.8f))
                          );

      }

      private Node StopTurning()
      {
        Debug.Log("StopTurning");
        return new Action(() => Turn(0));
      }

      private Node StopMoving()
      {
        Debug.Log("StopMoving");
        return new Action(() => Move(0));
      }

      private Node RandomFire()
      {
        return new Action(() => Fire(UnityEngine.Random.Range(0.0f, 1.0f)));
      }

    /* Example behaviour trees */

        // TankBehaviour
        private Root TankBehaviour(){
          return new Root(
                new Service(0.2f, UpdatePerception, DummyBehaviour())
                );
        } 

        // Constantly spin and fire on the spot 
        private Root SpinBehaviour(float turn, float shoot) {
            return new Root(new Sequence(
                        new Action(() => Turn(turn)),
                        new Action(() => Fire(shoot))
                    ));
        }

        // Turn to face your opponent and fire
        private Root TrackBehaviour() {
            return new Root(
                new Service(0.2f, UpdatePerception,
                    new Selector(
                        new BlackboardCondition("targetOffCentre",
                                                Operator.IS_SMALLER_OR_EQUAL, 0.1f,
                                                Stops.IMMEDIATE_RESTART,
                            // Stop turning and fire
                            new Sequence(StopTurning(),
                                        new Wait(2f),
                                        RandomFire())),
                        new BlackboardCondition("targetOnRight",
                                                Operator.IS_EQUAL, true,
                                                Stops.IMMEDIATE_RESTART,
                            // Turn right toward target
                            new Action(() => Turn(0.2f))),
                            // Turn left toward target
                            new Action(() => Turn(-0.2f))
                    )
                )
            );
        }

        private void UpdatePerception() {
     
            Vector3 targetPos = TargetTransform().position;
            Vector3 localPos = this.transform.InverseTransformPoint(targetPos);
            Vector3 heading = localPos.normalized;
            blackboard["targetDistance"] = localPos.magnitude;
            blackboard["targetInFront"] = heading.z > 0;
            blackboard["targetOnRight"] = heading.x > 0;
            blackboard["targetOffCentre"] = Mathf.Abs(heading.x);

            blackboard["lowHealth"] = m_Health.CurrentHealth < 40.0f;
            blackboard["resolvingBlockedPath"] = m_resolvingBlockedPath;
            blackboard["isBlocked"] = m_collisions > 0 || m_resolvingBlockedPath;

            //Debug.Log("UpdatePerception " + blackboard["isBlocked"].ToString());
    }

    }
}