@ui @local
Feature: Package smoke test
  One real browser scenario against the packed package. A green run proves the driver, the config
  reader, the tag policy, the page helpers and the artifact capture all work when HardPoint.Testing
  is consumed from its .nupkg rather than from source.

  Scenario: A page opens and a control updates the DOM
    Given the smoke page is open
    When I type "smoke" into the note field
    And I submit the smoke form
    Then the page reports "1 submission"
    And the driver saves a screenshot
