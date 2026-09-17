import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LikeButton } from './like-button';

describe('LikeButton', () => {
  let fixture: ComponentFixture<LikeButton>;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [LikeButton] });
    fixture = TestBed.createComponent(LikeButton);
    fixture.componentRef.setInput('liked', false);
    fixture.componentRef.setInput('count', 0);
  });

  function button(): HTMLButtonElement {
    return fixture.nativeElement.querySelector('button');
  }

  it('is disabled when the viewer is anonymous', () => {
    fixture.componentRef.setInput('disabled', true);
    fixture.componentRef.setInput('disabledReason', 'Log in to like');
    fixture.detectChanges();

    expect(button().disabled).toBe(true);
    expect(button().title).toBe('Log in to like');
  });

  it('is disabled for the post author (own post)', () => {
    fixture.componentRef.setInput('disabled', true);
    fixture.componentRef.setInput('disabledReason', 'You cannot like your own post');
    fixture.detectChanges();

    expect(button().disabled).toBe(true);
    expect(button().title).toBe('You cannot like your own post');
  });

  it('emits toggle on click when enabled', () => {
    fixture.componentRef.setInput('disabled', false);
    fixture.detectChanges();

    let emitted = false;
    fixture.componentInstance.toggle.subscribe(() => (emitted = true));

    button().click();

    expect(emitted).toBe(true);
  });
});
