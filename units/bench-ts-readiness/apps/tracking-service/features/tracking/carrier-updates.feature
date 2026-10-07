Feature: Carrier updates move a parcel through its statuses
  The worker reads each carrier's event codes and records them on the parcel. A parcel's status is the
  status of its latest event; an event the carrier repeats is recorded once.

  Background:
    Given merchant "merchant-a" has registered parcel "NPX12345678" with carrier "NPX"

  Scenario: A delivery is recorded
    When the carrier reports "AR" for parcel "NPX12345678" at "2026-10-05T06:00:00Z"
    And the carrier reports "OD" for parcel "NPX12345678" at "2026-10-05T07:10:00Z"
    And the carrier reports "DL" for parcel "NPX12345678" at "2026-10-05T11:42:00Z"
    Then parcel "NPX12345678" has status "delivered"
    And parcel "NPX12345678" has 3 tracking events

  Scenario: An exception is resolved and the parcel travels on
    When the carrier reports "EX" for parcel "NPX12345678" at "2026-10-05T06:00:00Z"
    Then parcel "NPX12345678" has status "exception"
    When the carrier reports "EXR" for parcel "NPX12345678" at "2026-10-05T09:30:00Z"
    Then parcel "NPX12345678" has status "in_transit"

  Scenario: A repeated event is recorded once
    When the carrier reports "AR" for parcel "NPX12345678" at "2026-10-05T06:00:00Z"
    And the carrier reports "AR" for parcel "NPX12345678" at "2026-10-05T06:00:00Z"
    Then parcel "NPX12345678" has 1 tracking event

  Scenario Outline: Carrier codes map to statuses
    When the carrier reports "<code>" for parcel "NPX12345678" at "2026-10-05T06:00:00Z"
    Then parcel "NPX12345678" has status "<status>"

    Examples:
      | code | status           |
      | DLV  | delivered        |
      | OD   | out_for_delivery |
      | RT   | returned         |
      | HB   | in_transit       |
