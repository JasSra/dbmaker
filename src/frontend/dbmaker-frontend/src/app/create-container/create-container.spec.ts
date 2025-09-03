import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CreateContainer } from './create-container';

describe('CreateContainer', () => {
  let component: CreateContainer;
  let fixture: ComponentFixture<CreateContainer>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CreateContainer]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CreateContainer);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
