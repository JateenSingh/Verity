import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TagControl } from './tag-control';

describe('TagControl', () => {
  let fixture: ComponentFixture<TagControl>;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [TagControl] });
    fixture = TestBed.createComponent(TagControl);
  });

  // Whether the control is shown at all is decided by the parent
  // (@if (authStore.isModerator())) - PostDetailPage owns that gating.
  // This component only controls the label/flow once it IS rendered.

  it('shows "Flag as misleading or false" when the post is not tagged', () => {
    fixture.componentRef.setInput('tagged', false);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Flag as misleading or false');
    expect(fixture.nativeElement.textContent).not.toContain('Remove flag');
  });

  it('shows "Remove flag" when the post is already tagged', () => {
    fixture.componentRef.setInput('tagged', true);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Remove flag');
    expect(fixture.nativeElement.textContent).not.toContain('Flag as misleading or false');
  });

  it('emits untag when "Remove flag" is clicked', () => {
    fixture.componentRef.setInput('tagged', true);
    fixture.detectChanges();

    let emitted = false;
    fixture.componentInstance.untag.subscribe(() => (emitted = true));
    fixture.nativeElement.querySelector('.tag-control__remove').click();

    expect(emitted).toBe(true);
  });

  it('opens a reason field and emits tag with the reason on confirm', () => {
    fixture.componentRef.setInput('tagged', false);
    fixture.detectChanges();

    fixture.nativeElement.querySelector('.tag-control__flag').click();
    fixture.detectChanges();

    const input: HTMLInputElement = fixture.nativeElement.querySelector('#tagReason');
    expect(input).toBeTruthy();

    input.value = 'Contradicts the docs';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    let emittedReason: string | undefined;
    fixture.componentInstance.tag.subscribe((reason) => (emittedReason = reason));
    fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));

    expect(emittedReason).toBe('Contradicts the docs');
  });
});
