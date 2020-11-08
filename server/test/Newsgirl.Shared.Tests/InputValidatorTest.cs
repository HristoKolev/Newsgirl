namespace Newsgirl.Shared.Tests
{
    using System.ComponentModel.DataAnnotations;
    using Testing;
    using Xunit;

    public class InputValidatorTest
    {
        [Fact]
        public void Validate_returns_success_when_the_object_has_no_attributes()
        {
            var result = InputValidator.Validate(new NoAttributeClass());
            Assert.True(result.IsOk);
        }

        [Fact]
        public void Validate_returns_fail_when_the_object_has_an_attribute_and_is_invalid()
        {
            var result = InputValidator.Validate(new ClassWithAttributes());
            Assert.False(result.IsOk);
        }

        [Fact]
        public void Validate_returns_success_when_the_object_has_an_attribute_and_is_valid()
        {
            var result = InputValidator.Validate(new ClassWithAttributes
            {
                Number = 1,
            });

            Assert.True(result.IsOk);
        }

        [Fact]
        public void Validate_validates_nested_types()
        {
            var result = InputValidator.Validate(new TestClassParent
            {
                Inner = new TestClassChild
                {
                    InnerNumber = null,
                },
            });

            Assert.False(result.IsOk);
        }

        [Fact]
        public void Validate_returns_error_messages()
        {
            var result = InputValidator.Validate(new TestClassParent
            {
                Inner = new TestClassChild(),
            });

            Assert.False(result.IsOk);

            var expectedMessages = new[] {"__OuterNumber__", "__InnerNumber__"};

            AssertExt.SequentialEqual(expectedMessages, result.ErrorMessages);
        }

        public class NoAttributeClass { }

        public class ClassWithAttributes
        {
            [Required]
            public int? Number { get; set; }
        }

        public class TestClassParent
        {
            [Required(ErrorMessage = "__OuterNumber__")]
            public int? OuterNumber { get; set; }

            [ValidateProperty]
            public TestClassChild Inner { get; set; }
        }

        public class TestClassChild
        {
            [Required(ErrorMessage = "__InnerNumber__")]
            public int? InnerNumber { get; set; }
        }
    }
}
